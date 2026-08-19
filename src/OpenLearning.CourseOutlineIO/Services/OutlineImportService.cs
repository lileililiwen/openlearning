using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseOutlineIO.Models;
using OpenLearning.Logging.Services;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.CourseOutlineIO.Services;

/// <summary>One parsed row of the outline workbook (data rows only).</summary>
public sealed record OutlineRow(
    int RowIndex,
    string? ModuleTitle,
    int? ModuleOrder,
    string? LessonTitle,
    int? LessonOrder,
    string? ContentUrl);

/// <summary>Result kind of an import request.</summary>
public enum OutlineImportOutcomeKind
{
    Error = 0,
    Completed = 1,
    Submitted = 2,
}

/// <summary>Result of an import request (sync result or async submission).</summary>
public sealed record OutlineImportOutcome(
    OutlineImportOutcomeKind Kind,
    string? Message,
    int? JobId,
    int TotalRows,
    int SuccessRows,
    IReadOnlyList<OutlineRowError> Errors);

/// <summary>Counts shown by the Replace-mode pre-flight confirmation.</summary>
public sealed record OutlineReplacePreview(int ModuleCount, int LessonCount);

/// <summary>
/// Excel import of a course outline (modules + lessons, metadata only). Sync up
/// to <c>courseOutline.import.syncMaxRows</c> (200); larger outlines go through
/// the async-io pipeline as kind <c>course-outline-import</c>, which delivers an
/// <c>import.completed</c> / <c>import.failed</c> notification.
/// </summary>
public class OutlineImportService : IAsyncIOProcessor
{
    public const string ImportKind = "course-outline-import";

    private const int _defaultMaxBytes = 5 * 1024 * 1024;
    private const int _defaultSyncMaxRows = 200;

    private readonly DbContext _db;
    private readonly AsyncIOService _asyncIO;
    private readonly StorageService _storage;
    private readonly SystemConfigService _config;
    private readonly LogService _logs;

    public OutlineImportService(
        DbContext db,
        AsyncIOService asyncIO,
        StorageService storage,
        SystemConfigService config,
        LogService logs)
    {
        _db = db;
        _asyncIO = asyncIO;
        _storage = storage;
        _config = config;
        _logs = logs;
    }

    public string Kind => ImportKind;

    public bool NeedsSourceFile => true;

    /// <summary>
    /// Validates the upload and ownership, then either runs the sync import or
    /// submits an async job. <paramref name="forceAsync"/> routes to async
    /// regardless of row count (the import-jobs page).
    /// </summary>
    public async Task<OutlineImportOutcome> ImportAsync(
        IFormFile? file,
        string ownerId,
        int courseId,
        OutlineImportMode mode,
        bool isAdmin,
        bool forceAsync)
    {
        if (!await CanImportAsync(courseId, ownerId, isAdmin))
        {
            return Error("您不是该课程的所有者，无法导入大纲。");
        }

        if (file is null || file.Length == 0)
        {
            return Error("请选择要上传的 .xlsx 文件。");
        }

        var validationError = await ValidateUploadAsync(file);
        if (validationError is not null)
        {
            return Error(validationError);
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        List<OutlineRow> rows;
        try
        {
            rows = ParseRows(stream);
        }
        catch (InvalidDataException ex)
        {
            return Error(ex.Message);
        }

        if (rows.Count == 0)
        {
            return Error("文件中没有数据行。");
        }

        var syncMax = await _config.GetIntAsync("courseOutline.import.syncMaxRows", _defaultSyncMaxRows);
        if (forceAsync || rows.Count > syncMax)
        {
            return await SubmitAsync(file, ownerId, courseId, mode);
        }

        return await ImportSyncCoreAsync(rows, ownerId, courseId, mode);
    }

    /// <summary>
    /// Async processor for kind <c>course-outline-import</c>: parses the stored
    /// file, validates, persists valid rows, writes the error file, and mirrors
    /// the outcome.
    /// </summary>
    public async Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream? fileStream, CancellationToken cancellationToken)
    {
        var meta = await _db.Set<OutlineImportJob>()
            .FirstOrDefaultAsync(j => j.AsyncIOJobId == job.Id, cancellationToken);
        if (meta is null)
        {
            return (false, "未找到导入任务元数据。", 0, 0);
        }

        if (fileStream is null)
        {
            return (false, "源文件缺失。", 0, 0);
        }

        meta.Status = OutlineImportJobStatus.Running;
        meta.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = ParseRows(fileStream);
            var errors = new List<OutlineRowError>();
            var success = 0;

            if (meta.Mode == OutlineImportMode.Replace)
            {
                await WipeOutlineAsync(meta.CourseId, cancellationToken);
            }

            // Persist valid rows grouped by module order (invalid rows → errors).
            await PersistRowsAsync(rows, errors, meta.CourseId, cancellationToken);
            success = rows.Count - errors.Count;
            await _db.SaveChangesAsync(cancellationToken);

            meta.TotalRows = rows.Count;
            meta.SuccessRows = success;
            meta.ErrorRows = errors.Count;
            meta.Status = OutlineImportJobStatus.Success;
            meta.FinishedAt = DateTime.UtcNow;

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    error.JobId = meta.Id;
                    _db.Set<OutlineRowError>().Add(error);
                }

                await _db.SaveChangesAsync(cancellationToken);
                var errorKey = await WriteErrorFileAsync(job.Id, meta.UserId, errors);
                meta.ErrorFileKey = errorKey;
                job.ErrorFileKey = errorKey;
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _logs.RecordAsync(
                meta.UserId,
                string.Empty,
                "OutlineImport",
                "OutlineImportJob",
                meta.Id.ToString(CultureInfo.InvariantCulture),
                $"course={meta.CourseId}, mode={meta.Mode}, file={meta.FileKey}, total={meta.TotalRows}, ok={meta.SuccessRows}, errors={meta.ErrorRows}",
                null);
            return (true, null, rows.Count, success);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            meta.Status = OutlineImportJobStatus.Failed;
            meta.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return (false, ex.Message, 0, 0);
        }
    }

    /// <summary>Counts of modules/lessons that a Replace import would delete.</summary>
    public async Task<OutlineReplacePreview> PreflightReplaceAsync(int courseId)
    {
        var lessonCount = await _db.Set<Lesson>().AsNoTracking()
            .CountAsync(l => l.Module!.CourseId == courseId);
        var moduleCount = await _db.Set<Module>().AsNoTracking()
            .CountAsync(m => m.CourseId == courseId);
        return new OutlineReplacePreview(moduleCount, lessonCount);
    }

    private async Task<bool> CanImportAsync(int courseId, string ownerId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _db.Set<Course>().AnyAsync(c => c.Id == courseId);
        }

        return await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == ownerId);
    }

    private async Task<OutlineImportOutcome> ImportSyncCoreAsync(
        IReadOnlyList<OutlineRow> rows,
        string ownerId,
        int courseId,
        OutlineImportMode mode)
    {
        var errors = new List<OutlineRowError>();
        if (mode == OutlineImportMode.Replace)
        {
            await WipeOutlineAsync(courseId, default);
        }

        await PersistRowsAsync(rows, errors, courseId, default);
        var success = rows.Count - errors.Count;
        await _db.SaveChangesAsync();

        await _logs.RecordAsync(
            ownerId,
            string.Empty,
            "OutlineImport",
            "OutlineImportJob",
            "-",
            $"course={courseId}, mode={mode}, total={rows.Count}, ok={success}, errors={errors.Count}",
            null);
        return new OutlineImportOutcome(
            OutlineImportOutcomeKind.Completed,
            null,
            null,
            rows.Count,
            success,
            errors);
    }

    private async Task<OutlineImportOutcome> SubmitAsync(
        IFormFile file, string ownerId, int courseId, OutlineImportMode mode)
    {
        var (job, error) = await _asyncIO.SubmitAsync(ownerId, Kind, new XlsxFileValidator(), file);
        if (error is not null || job is null)
        {
            return Error(error ?? "提交导入任务失败。");
        }

        var importJob = new OutlineImportJob
        {
            UserId = ownerId,
            CourseId = courseId,
            Mode = mode,
            FileKey = job.FileKey,
            AsyncIOJobId = job.Id,
        };
        _db.Set<OutlineImportJob>().Add(importJob);
        await _db.SaveChangesAsync();

        return new OutlineImportOutcome(
            OutlineImportOutcomeKind.Submitted,
            null,
            job.Id,
            0,
            0,
            Array.Empty<OutlineRowError>());
    }

    private async Task WipeOutlineAsync(int courseId, CancellationToken ct)
    {
        var modules = await _db.Set<Module>()
            .Where(m => m.CourseId == courseId)
            .ToListAsync(ct);
        if (modules.Count > 0)
        {
            _db.Set<Module>().RemoveRange(modules);
            // Commit the wipe before re-importing so the persist step does not
            // see the to-be-deleted rows as existing modules.
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Creates the modules and lessons for the valid rows (grouped by module
    /// order). Invalid rows are collected into <paramref name="errors"/>.
    /// </summary>
    private async Task PersistRowsAsync(
        IReadOnlyList<OutlineRow> rows,
        List<OutlineRowError> errors,
        int courseId,
        CancellationToken ct)
    {
        var valid = new List<OutlineRow>();
        foreach (var row in rows)
        {
            var rowErrors = ValidateRow(row, rows);
            if (rowErrors.Count > 0)
            {
                foreach (var error in rowErrors)
                {
                    errors.Add(error);
                }
            }
            else
            {
                valid.Add(row);
            }
        }

        if (valid.Count == 0)
        {
            return;
        }

        // Resolve the module for each distinct module order (first title wins).
        var moduleOrders = valid
            .Where(r => r.ModuleOrder is not null)
            .Select(r => r.ModuleOrder!.Value)
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        var moduleByOrder = new Dictionary<int, Module>();
        var existing = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .ToListAsync(ct);
        var existingOrders = existing.Select(m => m.OrderIndex).ToHashSet();

        foreach (var order in moduleOrders)
        {
            var title = valid.First(r => r.ModuleOrder == order && !string.IsNullOrWhiteSpace(r.ModuleTitle)).ModuleTitle!.Trim();
            if (existingOrders.Contains(order))
            {
                // Append into an existing module slot: reuse it rather than duplicate.
                var existingModule = existing.First(m => m.OrderIndex == order);
                moduleByOrder[order] = existingModule;
            }
            else
            {
                var module = new Module { CourseId = courseId, Title = title, OrderIndex = order };
                _db.Set<Module>().Add(module);
                moduleByOrder[order] = module;
            }
        }

        if (moduleByOrder.Count == 0)
        {
            return;
        }

        // Need module ids before attaching lessons — flush now.
        await _db.SaveChangesAsync(ct);

        foreach (var row in valid.Where(r => !string.IsNullOrWhiteSpace(r.LessonTitle)))
        {
            ct.ThrowIfCancellationRequested();
            if (row.ModuleOrder is not int moduleOrder || !moduleByOrder.TryGetValue(moduleOrder, out var module))
            {
                continue;
            }

            _db.Set<Lesson>().Add(new Lesson
            {
                ModuleId = module.Id,
                Title = row.LessonTitle!.Trim(),
                OrderIndex = row.LessonOrder ?? 0,
                ContentUrlRef = string.IsNullOrWhiteSpace(row.ContentUrl) ? null : row.ContentUrl.Trim(),
            });
        }
    }

    private async Task<string?> ValidateUploadAsync(IFormFile file)
    {
        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return "仅支持 .xlsx 文件。";
        }

        var maxBytes = await _config.GetIntAsync("courseOutline.import.maxBytes", _defaultMaxBytes);
        return file.Length > maxBytes ? $"文件不能超过 {maxBytes / 1024 / 1024} MB。" : null;
    }

    private static List<OutlineRow> ParseRows(Stream stream)
    {
        var rows = new List<OutlineRow>();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 2)
        {
            return rows;
        }

        for (var r = 2; r <= lastRow; r++)
        {
            var moduleTitle = ReadString(sheet, r, 1);
            var moduleOrder = ReadInt(sheet, r, 2);
            var lessonTitle = ReadString(sheet, r, 3);
            var lessonOrder = ReadInt(sheet, r, 4);
            var contentUrl = ReadString(sheet, r, 5);

            if (string.IsNullOrWhiteSpace(moduleTitle)
                && string.IsNullOrWhiteSpace(lessonTitle)
                && moduleOrder is null
                && lessonOrder is null
                && string.IsNullOrWhiteSpace(contentUrl))
            {
                continue; // blank row
            }

            rows.Add(new OutlineRow(r, moduleTitle, moduleOrder, lessonTitle, lessonOrder, contentUrl));
        }

        return rows;
    }

    /// <summary>Validates one row per the spec rules; duplicates are file-wide.</summary>
    private static List<OutlineRowError> ValidateRow(OutlineRow row, IReadOnlyList<OutlineRow> all)
    {
        var errors = new List<OutlineRowError>();
        if (string.IsNullOrWhiteSpace(row.ModuleTitle))
        {
            errors.Add(new OutlineRowError { RowIndex = row.RowIndex, Field = "ModuleTitle", Message = "模块标题不能为空。" });
        }

        if (row.ModuleOrder is null || row.ModuleOrder < 0)
        {
            errors.Add(new OutlineRowError { RowIndex = row.RowIndex, Field = "ModuleOrder", Message = "模块顺序必须是非负整数。" });
        }

        if (!string.IsNullOrWhiteSpace(row.LessonTitle))
        {
            if (row.LessonOrder is null || row.LessonOrder < 0)
            {
                errors.Add(new OutlineRowError { RowIndex = row.RowIndex, Field = "LessonOrder", Message = "课时顺序必须是非负整数。" });
            }
            else if (row.ModuleOrder is not null)
            {
                var duplicate = all.Any(x =>
                    x.RowIndex < row.RowIndex
                    && !string.IsNullOrWhiteSpace(x.LessonTitle)
                    && x.ModuleOrder == row.ModuleOrder
                    && x.LessonOrder == row.LessonOrder);
                if (duplicate)
                {
                    errors.Add(new OutlineRowError { RowIndex = row.RowIndex, Field = "LessonOrder", Message = "重复的课时顺序。" });
                }
            }
        }

        if (row.ContentUrl is not null && row.ContentUrl.Length > 2000)
        {
            errors.Add(new OutlineRowError { RowIndex = row.RowIndex, Field = "LessonContentUrl", Message = "内容地址不能超过 2000 个字符。" });
        }

        return errors;
    }

    private async Task<string?> WriteErrorFileAsync(int asyncIOJobId, string ownerId, IReadOnlyList<OutlineRowError> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Errors");
            sheet.Cell(1, 1).Value = "Row";
            sheet.Cell(1, 2).Value = "Field";
            sheet.Cell(1, 3).Value = "Message";
            var row = 2;
            foreach (var error in errors)
            {
                sheet.Cell(row, 1).Value = error.RowIndex;
                sheet.Cell(row, 2).Value = error.Field;
                sheet.Cell(row, 3).Value = error.Message;
                row++;
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var (file, uploadError) = await _storage.UploadAsync(
            ownerId, OpenLearning.Storage.Models.FilePurpose.AsyncIO, $"outline-errors-{asyncIOJobId}.xlsx", "application/octet-stream", stream);
        if (uploadError is null && file is not null)
        {
            return file.Key;
        }

        return null;
    }

    private static string? ReadString(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (!cell.IsEmpty())
        {
            return cell.GetFormattedString();
        }

        return null;
    }

    private static int? ReadInt(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return (int)Math.Round(cell.GetDouble());
        }

        var text = cell.GetFormattedString();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static OutlineImportOutcome Error(string message)
    {
        return new OutlineImportOutcome(OutlineImportOutcomeKind.Error, message, null, 0, 0, Array.Empty<OutlineRowError>());
    }

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
