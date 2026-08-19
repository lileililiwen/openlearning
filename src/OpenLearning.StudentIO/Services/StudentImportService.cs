using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Logging.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using OpenLearning.StudentIO.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.StudentIO.Services;

public enum StudentImportOutcomeKind
{
    Error,
    Submitted,
    Completed,
}

/// <summary>One parsed row as shown in the import page preview.</summary>
public sealed record StudentRowPreview(int RowIndex, string Email, string Action, bool Ok, string? Error);

/// <summary>Result of an import submission (sync result, async job id, or rejection).</summary>
public sealed record StudentImportOutcome(
    StudentImportOutcomeKind Kind,
    string? Error,
    int? JobId,
    int TotalRows,
    int SuccessCount,
    IReadOnlyList<StudentImportRowError> Errors,
    IReadOnlyList<StudentRowPreview> Preview);

/// <summary>
/// Bulk student import. Small files (≤200 rows) run synchronously with a
/// row-by-row error report; larger files are submitted to the async-io
/// pipeline as kind <c>student-import</c>. Each row has its own action
/// (Create / CreateAndEnroll / EnrollExisting). Admin/Finance import without
/// scope; TA imports are restricted to class groups they are assigned to.
/// </summary>
public class StudentImportService : IAsyncIOProcessor
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int _defaultMaxBytes = 10 * 1024 * 1024;
    private const int _defaultSyncMaxRows = 200;

    private readonly DbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly StorageService _storage;
    private readonly AsyncIOService _asyncIO;
    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;
    private readonly IClassAssignmentLookup _classLookup;

    public StudentImportService(
        DbContext db,
        UserManager<ApplicationUser> users,
        StorageService storage,
        AsyncIOService asyncIO,
        EnrollmentService enrollments,
        NotificationService notifications,
        IClassAssignmentLookup classLookup)
    {
        _db = db;
        _users = users;
        _storage = storage;
        _asyncIO = asyncIO;
        _enrollments = enrollments;
        _notifications = notifications;
        _classLookup = classLookup;
    }

    public string Kind => "student-import";

    public bool NeedsSourceFile => true;

    /// <summary>
    /// Validates the upload, then either runs the sync import or submits an
    /// async job. <paramref name="forceAsync"/> routes to async regardless of
    /// row count. <paramref name="defaultAction"/> applies to rows with a blank
    /// Action column.
    /// </summary>
    public async Task<StudentImportOutcome> ImportAsync(
        IFormFile? file,
        string importerId,
        StudentImportScope scope,
        StudentRowAction defaultAction,
        bool forceAsync)
    {
        if (file is null || file.Length == 0)
        {
            return Fail("请选择要上传的 .xlsx 文件。");
        }

        var validationError = ValidateUpload(file);
        if (validationError is not null)
        {
            return Fail(validationError);
        }

        if (scope.IsTa && scope.RequiredClassGroupId is int requiredClass)
        {
            var assigned = await _classLookup.IsAssignedAsync(importerId, requiredClass);
            if (!assigned)
            {
                return Fail("您未被分配到此班级。");
            }
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        List<StudentParsedRow> rows;
        try
        {
            rows = ParseRows(stream);
        }
        catch (InvalidDataException ex)
        {
            return Fail(ex.Message);
        }

        if (rows.Count == 0)
        {
            return Fail("文件中没有数据行。");
        }

        if (forceAsync || rows.Count > _defaultSyncMaxRows)
        {
            return await SubmitAsync(file, importerId, SummarizeMode(rows, defaultAction), defaultAction);
        }

        var (success, errors, previews) = await ProcessRowsAsync(rows, importerId, scope, defaultAction);
        await WriteAuditAsync(importerId, rows.Count, success, errors.Count, fileKey: null, jobId: null);
        await _db.SaveChangesAsync();

        return new StudentImportOutcome(
            StudentImportOutcomeKind.Completed,
            null,
            null,
            rows.Count,
            success,
            errors,
            previews);
    }

    /// <summary>
    /// Async processor invoked by the async-io dispatcher for kind
    /// <c>student-import</c>: parses the stored file, executes each row, writes
    /// the error file, and mirrors the outcome.
    /// </summary>
    public async Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream? fileStream, CancellationToken cancellationToken)
    {
        var meta = await _db.Set<StudentImportJob>().FirstOrDefaultAsync(j => j.AsyncIOJobId == job.Id, cancellationToken);
        if (meta is null)
        {
            return (false, "未找到导入任务元数据。", 0, 0);
        }

        if (fileStream is null)
        {
            return (false, "源文件缺失。", 0, 0);
        }

        meta.Status = StudentImportJobStatus.Running;
        meta.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = ParseRows(fileStream);
            var scope = new StudentImportScope(IsTa: false);
            var (success, errors, _) = await ProcessRowsAsync(rows, meta.UserId, scope, meta.DefaultAction, cancellationToken);

            meta.TotalRows = rows.Count;
            meta.SuccessRows = success;
            meta.ErrorRows = errors.Count;
            meta.Status = StudentImportJobStatus.Success;
            meta.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    error.JobId = meta.Id;
                    _db.Set<StudentImportRowError>().Add(error);
                }

                await _db.SaveChangesAsync(cancellationToken);
                var errorKey = await WriteErrorFileAsync(job.Id, meta.UserId, errors);
                meta.ErrorFileKey = errorKey;
                job.ErrorFileKey = errorKey;
                await _db.SaveChangesAsync(cancellationToken);
            }

            await WriteAuditAsync(meta.UserId, rows.Count, success, errors.Count, meta.FileKey, job.Id);
            await _db.SaveChangesAsync(cancellationToken);

            return (true, null, rows.Count, success);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            meta.Status = StudentImportJobStatus.Failed;
            meta.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return (false, ex.Message, 0, 0);
        }
    }

    private async Task<StudentImportOutcome> SubmitAsync(
        IFormFile file, string importerId, StudentImportMode mode, StudentRowAction defaultAction)
    {
        var (job, error) = await _asyncIO.SubmitAsync(importerId, Kind, new XlsxFileValidator(), file);
        if (error is not null || job is null)
        {
            return Fail(error ?? "提交导入任务失败。");
        }

        var importJob = new StudentImportJob
        {
            UserId = importerId,
            Mode = mode,
            DefaultAction = defaultAction,
            FileKey = job.FileKey,
            AsyncIOJobId = job.Id,
        };
        _db.Set<StudentImportJob>().Add(importJob);
        await _db.SaveChangesAsync();

        return new StudentImportOutcome(
            StudentImportOutcomeKind.Submitted,
            null,
            job.Id,
            0,
            0,
            Array.Empty<StudentImportRowError>(),
            Array.Empty<StudentRowPreview>());
    }

    private async Task<(int Success, List<StudentImportRowError> Errors, List<StudentRowPreview> Previews)> ProcessRowsAsync(
        IReadOnlyList<StudentParsedRow> rows,
        string importerId,
        StudentImportScope scope,
        StudentRowAction defaultAction,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<StudentImportRowError>();
        var previews = new List<StudentRowPreview>();
        var duplicateEmails = FindDuplicateEmails(rows);
        var success = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var email = row.Email?.Trim() ?? string.Empty;
            if (email.Length > 0 && duplicateEmails.Contains(email))
            {
                var message = "duplicate email";
                errors.Add(RowError(row, "Email", message));
                previews.Add(new StudentRowPreview(row.RowIndex, email, row.ActionText ?? string.Empty, false, message));
                continue;
            }

            var (rowErrors, input) = ValidateRow(row, defaultAction, scope.IsTa);
            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                previews.Add(new StudentRowPreview(row.RowIndex, email, row.ActionText ?? string.Empty, false, rowErrors[0].Message));
                continue;
            }

            var result = await ExecuteRowAsync(input!, importerId, scope);
            if (result.Error is not null)
            {
                errors.Add(RowError(row, result.Field, result.Error));
                previews.Add(new StudentRowPreview(row.RowIndex, email, input!.Action.ToString(), false, result.Error));
            }
            else
            {
                success++;
                previews.Add(new StudentRowPreview(row.RowIndex, email, input!.Action.ToString(), true, null));
            }
        }

        return (success, errors, previews);
    }

    private async Task<(string? Error, string Field)> ExecuteRowAsync(
        StudentRowInput input, string importerId, StudentImportScope scope)
    {
        ApplicationUser? user = null;
        string? resetLink = null;

        switch (input.Action)
        {
            case StudentRowAction.Create:
            case StudentRowAction.CreateAndEnroll:
                {
                    var created = await CreateUserAsync(input);
                    if (created.Error is not null)
                    {
                        return (created.Error, "Email");
                    }

                    user = created.User;
                    if (string.IsNullOrWhiteSpace(input.Password))
                    {
                        resetLink = await BuildResetLinkAsync(user!);
                    }

                    break;
                }

            case StudentRowAction.EnrollExisting:
                {
                    user = await FindUserByEmailAsync(input.Email);
                    if (user is null)
                    {
                        return ("user not found", "Email");
                    }

                    break;
                }
        }

        var enrolledTitles = new List<string>();
        string? enrollError = null;
        if (input.Action is StudentRowAction.CreateAndEnroll or StudentRowAction.EnrollExisting)
        {
            enrollError = await EnrollAsync(user!, input, importerId, scope, enrolledTitles);
        }

        if (input.Action is StudentRowAction.Create or StudentRowAction.CreateAndEnroll)
        {
            await _notifications.SendAsync(
                NotificationService.EventKeys.AccountWelcome,
                user!.Id,
                new Dictionary<string, string>
                {
                    ["displayName"] = user.DisplayName,
                    ["courseList"] = string.Join(", ", enrolledTitles),
                },
                resetLink);
        }
        else if (enrolledTitles.Count > 0)
        {
            await _notifications.SendAsync(
                NotificationService.EventKeys.EnrollmentGrantedBulk,
                user!.Id,
                new Dictionary<string, string> { ["courseList"] = string.Join(", ", enrolledTitles) });
        }

        return (enrollError, "CourseIds");
    }

    private async Task<string?> EnrollAsync(
        ApplicationUser student,
        StudentRowInput input,
        string importerId,
        StudentImportScope scope,
        List<string> enrolledTitles)
    {
        if (scope.IsTa)
        {
            foreach (var classGroupId in input.ClassGroupIds)
            {
                if (!await _classLookup.IsAssignedAsync(importerId, classGroupId))
                {
                    return "class not assigned";
                }
            }
        }

        var classGroups = await _db.Set<ClassGroup>().AsNoTracking()
            .Where(c => input.ClassGroupIds.Contains(c.Id))
            .ToListAsync();

        foreach (var courseId in input.CourseIds)
        {
            var course = await _db.Set<Course>().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);
            if (course is null || course.Status != CourseStatus.Published)
            {
                return "course not found or not published";
            }

            if (course.Price is > 0)
            {
                var paid = await _db.Set<Order>().AnyAsync(o =>
                    o.StudentId == student.Id && o.CourseId == courseId && o.Status == OrderStatus.Paid);
                if (!paid)
                {
                    return "course requires purchase";
                }
            }

            var (ok, error) = await _enrollments.EnrollAsync(student.Id, courseId);
            if (!ok)
            {
                return error ?? "enrollment failed";
            }

            var classGroup = classGroups.FirstOrDefault(c => c.CourseId == courseId);
            if (classGroup is not null)
            {
                var enrollment = await _db.Set<EnrollmentEntity>()
                    .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.RevokedAt == null);
                if (enrollment is not null)
                {
                    enrollment.ClassGroupId = classGroup.Id;
                    await _db.SaveChangesAsync();
                }
            }

            enrolledTitles.Add(course.Title);
        }

        return null;
    }

    private async Task<(ApplicationUser? User, string? Error)> CreateUserAsync(StudentRowInput input)
    {
        var normalized = input.Email.Trim().ToUpperInvariant();
        if (await _db.Set<ApplicationUser>().AnyAsync(u => u.NormalizedEmail == normalized))
        {
            return (null, "email already in use");
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            DisplayName = input.DisplayName,
            PhoneNumber = input.Phone,
        };
        IdentityResult result = string.IsNullOrWhiteSpace(input.Password)
            ? await _users.CreateAsync(user)
            : await _users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            return (null, result.Errors.FirstOrDefault()?.Description ?? "account creation failed");
        }

        await _users.AddToRoleAsync(user, Roles.Student);
        return (user, null);
    }

    private Task<ApplicationUser?> FindUserByEmailAsync(string email)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return _db.Set<ApplicationUser>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);
    }

    private async Task<string?> BuildResetLinkAsync(ApplicationUser user)
    {
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        return $"/Auth/ResetPassword?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(token)}";
    }

    private async Task WriteAuditAsync(string importerId, int total, int success, int errors, string? fileKey, int? jobId)
    {
        var importerName = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == importerId)
            .Select(u => u.Email ?? u.UserName ?? string.Empty)
            .FirstOrDefaultAsync() ?? string.Empty;

        _db.Set<OperationLog>().Add(new OperationLog
        {
            ActorId = importerId,
            ActorName = importerName,
            Action = "StudentImport",
            TargetType = "StudentImportJob",
            TargetId = jobId?.ToString(CultureInfo.InvariantCulture) ?? "-",
            Details = $"file={fileKey}, rows={total}, success={success}, errors={errors}",
            IpAddress = null,
        });
    }

    private static (List<StudentImportRowError> Errors, StudentRowInput? Input) ValidateRow(
        StudentParsedRow row, StudentRowAction defaultAction, bool isTa)
    {
        var errors = new List<StudentImportRowError>();

        var actionText = row.ActionText?.Trim();
        StudentRowAction action;
        if (string.IsNullOrWhiteSpace(actionText))
        {
            action = defaultAction;
        }
        else if (ParseRowAction(actionText) is StudentRowAction parsed)
        {
            action = parsed;
        }
        else
        {
            errors.Add(RowError(row, "Action", "不支持的操作，允许的值：Create, CreateAndEnroll, EnrollExisting。"));
            return (errors, null);
        }

        var email = row.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
        {
            errors.Add(RowError(row, "Email", "邮箱格式不正确。"));
        }

        var password = row.Password;
        if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
        {
            errors.Add(RowError(row, "Password", "密码至少 8 个字符。"));
        }

        var courseIds = ParseIdList(row.CourseIdsText);
        if (courseIds is null)
        {
            errors.Add(RowError(row, "CourseIds", "CourseIds 必须是分号分隔的整数列表。"));
        }

        var classGroupIds = ParseIdList(row.ClassGroupIdsText);
        if (classGroupIds is null)
        {
            errors.Add(RowError(row, "ClassGroupIds", "ClassGroupIds 必须是分号分隔的整数列表。"));
        }

        if (isTa && action == StudentRowAction.Create)
        {
            errors.Add(RowError(row, "Action", "TA 只能使用 CreateAndEnroll 或 EnrollExisting。"));
        }

        if (isTa && (classGroupIds is null || classGroupIds.Count == 0))
        {
            errors.Add(RowError(row, "ClassGroupIds", "TA 导入必须指定班级。"));
        }

        if (errors.Count > 0)
        {
            return (errors, null);
        }

        var displayName = row.DisplayName?.Trim();
        var input = new StudentRowInput(
            action,
            email,
            string.IsNullOrWhiteSpace(row.Phone?.Trim()) ? null : row.Phone.Trim(),
            string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName,
            string.IsNullOrWhiteSpace(password) ? null : password,
            courseIds!,
            classGroupIds!);
        return (errors, input);
    }

    private static StudentRowAction? ParseRowAction(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Trim().ToLowerInvariant() switch
        {
            "create" => StudentRowAction.Create,
            "createandenroll" or "create_and_enroll" => StudentRowAction.CreateAndEnroll,
            "enrollexisting" or "enroll_existing" => StudentRowAction.EnrollExisting,
            _ => null,
        };
    }

    private static StudentImportMode SummarizeMode(IReadOnlyList<StudentParsedRow> rows, StudentRowAction defaultAction)
    {
        var sawCreate = false;
        var sawEnroll = false;
        foreach (var row in rows)
        {
            var action = ParseRowAction(row.ActionText) ?? defaultAction;
            if (action == StudentRowAction.Create)
            {
                sawCreate = true;
            }
            else
            {
                sawEnroll = true;
            }
        }

        if (sawCreate && sawEnroll)
        {
            return StudentImportMode.Mixed;
        }

        return sawCreate ? StudentImportMode.Create : StudentImportMode.Enroll;
    }

    private static List<int>? ParseIdList(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<int>();
        }

        var result = new List<int>();
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var value))
            {
                return null;
            }

            result.Add(value);
        }

        return result;
    }

    private static HashSet<string> FindDuplicateEmails(IReadOnlyList<StudentParsedRow> rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var email = row.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            if (!seen.Add(email))
            {
                duplicates.Add(email);
            }
        }

        return duplicates;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static StudentImportRowError RowError(StudentParsedRow row, string field, string message)
    {
        return new StudentImportRowError { RowIndex = row.RowIndex, Field = field, Message = message };
    }

    private static StudentImportOutcome Fail(string message)
    {
        return new StudentImportOutcome(
            StudentImportOutcomeKind.Error,
            message,
            null,
            0,
            0,
            Array.Empty<StudentImportRowError>(),
            Array.Empty<StudentRowPreview>());
    }

    private static string? ValidateUpload(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return "仅支持 .xlsx 文件。";
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) && !file.ContentType.Equals(_contentTypeXlsx, StringComparison.OrdinalIgnoreCase))
        {
            return "仅支持 .xlsx 文件。";
        }

        if (file.Length > _defaultMaxBytes)
        {
            return $"文件超过大小限制（{_defaultMaxBytes / (1024 * 1024)} MB）。";
        }

        return null;
    }

    private async Task<string?> WriteErrorFileAsync(int asyncIOJobId, string ownerId, IReadOnlyList<StudentImportRowError> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Errors");
            sheet.Cell(1, 1).Value = "RowIndex";
            sheet.Cell(1, 2).Value = "Field";
            sheet.Cell(1, 3).Value = "Message";
            for (var i = 0; i < errors.Count; i++)
            {
                sheet.Cell(i + 2, 1).Value = errors[i].RowIndex;
                sheet.Cell(i + 2, 2).Value = errors[i].Field;
                sheet.Cell(i + 2, 3).Value = errors[i].Message;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var (file, error) = await _storage.UploadAsync(ownerId, FilePurpose.AsyncIO, $"errors-{asyncIOJobId}.xlsx", _contentTypeXlsx, stream);
        return error is null && file is not null ? file.Key : null;
    }

    private static List<StudentParsedRow> ParseRows(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidDataException("工作簿中没有工作表。");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastHeaderColumn = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var c = 1; c <= lastHeaderColumn; c++)
        {
            var name = sheet.Cell(1, c).GetString().Trim();
            if (name.Length > 0 && !headers.ContainsKey(name))
            {
                headers[name] = c;
            }
        }

        if (!headers.ContainsKey("Email"))
        {
            throw new InvalidDataException("文件缺少必需列（Email）。");
        }

        var rows = new List<StudentParsedRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var values = headers.ToDictionary(h => h.Key, h => CellText(sheet.Cell(r, h.Value)), StringComparer.OrdinalIgnoreCase);
            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new StudentParsedRow(
                r,
                values.GetValueOrDefault("Action"),
                values.GetValueOrDefault("Email"),
                values.GetValueOrDefault("Phone"),
                values.GetValueOrDefault("DisplayName"),
                values.GetValueOrDefault("Password"),
                values.GetValueOrDefault("CourseIds"),
                values.GetValueOrDefault("ClassGroupIds")));
        }

        return rows;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        var value = cell.Value;
        return value.Type switch
        {
            XLDataType.Number => value.GetNumber().ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
            XLDataType.Boolean => value.GetBoolean() ? "True" : "False",
            XLDataType.DateTime => value.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.GetText(),
        };
    }

    /// <summary>Allows the async-io submit path to re-validate cheaply (size was already enforced).</summary>
    private sealed class XlsxFileValidator : IIOFileValidator
    {
        public string[] AllowedExtensions { get; } = [".xlsx"];

        public long MaxBytes => long.MaxValue;

        public string? Validate(IFormFile file)
        {
            return string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)
                ? null
                : "仅支持 .xlsx 文件。";
        }
    }
}
