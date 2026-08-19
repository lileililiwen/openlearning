using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.CouponIO.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Logging.Services;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.CouponIO.Services;

/// <summary>One parsed row of the coupon workbook (data rows only).</summary>
public sealed record CouponImportRow(
    int RowIndex,
    string? Code,
    string? DiscountType,
    decimal? DiscountValue,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int? MaxRedemptions);

/// <summary>Result kind of a coupon import request.</summary>
public enum CouponImportOutcomeKind
{
    Error = 0,
    RateLimited = 1,
    Completed = 2,
    Submitted = 3,
}

/// <summary>Result of a coupon import request (sync result or async submission).</summary>
public sealed record CouponImportOutcome(
    CouponImportOutcomeKind Kind,
    string? Message,
    int? RetryAfterSeconds,
    int? JobId,
    int TotalRows,
    int SuccessRows,
    IReadOnlyList<CouponImportRowError> Errors);

/// <summary>
/// Bulk creation of coupon codes from an Excel workbook (append-only; unique
/// codes enforced server-side). Sync up to <c>coupon.import.syncMaxRows</c>
/// (200); larger imports go through the async-io pipeline as kind
/// <c>coupon-import</c>, which delivers an <c>import.completed</c> /
/// <c>import.failed</c> notification.
/// </summary>
public partial class CouponImportService : IAsyncIOProcessor
{
    public const string ImportKind = "coupon-import";

    private const int _defaultMaxBytes = 5 * 1024 * 1024;
    private const int _defaultSyncMaxRows = 200;

    [GeneratedRegex("^[A-Za-z0-9_-]{4,32}$")]
    private static partial Regex CodeRegex();

    private readonly DbContext _db;
    private readonly AsyncIOService _asyncIO;
    private readonly StorageService _storage;
    private readonly SystemConfigService _config;
    private readonly LogService _logs;
    private readonly CouponImportRateLimiter _rateLimiter;

    public CouponImportService(
        DbContext db,
        AsyncIOService asyncIO,
        StorageService storage,
        SystemConfigService config,
        LogService logs,
        CouponImportRateLimiter rateLimiter)
    {
        _db = db;
        _asyncIO = asyncIO;
        _storage = storage;
        _config = config;
        _logs = logs;
        _rateLimiter = rateLimiter;
    }

    public string Kind => ImportKind;

    public bool NeedsSourceFile => true;

    /// <summary>
    /// Validates the upload and rate limit, then either runs the sync import or
    /// submits an async job. <paramref name="forceAsync"/> routes to async
    /// regardless of row count.
    /// </summary>
    public async Task<CouponImportOutcome> ImportAsync(IFormFile? file, string adminId, bool forceAsync)
    {
        if (file is null || file.Length == 0)
        {
            return Error("请选择要上传的 .xlsx 文件。");
        }

        var validationError = await ValidateUploadAsync(file);
        if (validationError is not null)
        {
            return Error(validationError);
        }

        var rate = await _rateLimiter.CheckAsync(adminId);
        if (!rate.Allowed)
        {
            return new CouponImportOutcome(
                CouponImportOutcomeKind.RateLimited,
                $"导入过于频繁，请在 {rate.RetryAfterSeconds} 秒后重试。",
                rate.RetryAfterSeconds,
                null,
                0,
                0,
                Array.Empty<CouponImportRowError>());
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        List<CouponImportRow> rows;
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

        var syncMax = await _config.GetIntAsync("coupon.import.syncMaxRows", _defaultSyncMaxRows);
        if (forceAsync || rows.Count > syncMax)
        {
            return await SubmitAsync(file, adminId);
        }

        return await ImportSyncCoreAsync(rows, adminId);
    }

    /// <summary>
    /// Async processor for kind <c>coupon-import</c>: parses the stored file,
    /// validates, persists valid rows, writes the error file, and mirrors the
    /// outcome.
    /// </summary>
    public async Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream? fileStream, CancellationToken cancellationToken)
    {
        var meta = await _db.Set<CouponImportJob>()
            .FirstOrDefaultAsync(j => j.AsyncIOJobId == job.Id, cancellationToken);
        if (meta is null)
        {
            return (false, "未找到导入任务元数据。", 0, 0);
        }

        if (fileStream is null)
        {
            return (false, "源文件缺失。", 0, 0);
        }

        meta.Status = CouponImportJobStatus.Running;
        meta.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = ParseRows(fileStream);
            var (errors, success) = await ValidateAndPersistAsync(rows, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            meta.TotalRows = rows.Count;
            meta.SuccessRows = success;
            meta.ErrorRows = errors.Count;
            meta.Status = CouponImportJobStatus.Success;
            meta.FinishedAt = DateTime.UtcNow;

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    error.JobId = meta.Id;
                    _db.Set<CouponImportRowError>().Add(error);
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
                "CouponImport",
                "CouponImportJob",
                meta.Id.ToString(CultureInfo.InvariantCulture),
                $"file={meta.FileKey}, total={meta.TotalRows}, ok={meta.SuccessRows}, errors={meta.ErrorRows}",
                null);
            return (true, null, rows.Count, success);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            meta.Status = CouponImportJobStatus.Failed;
            meta.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return (false, ex.Message, 0, 0);
        }
    }

    private async Task<CouponImportOutcome> ImportSyncCoreAsync(IReadOnlyList<CouponImportRow> rows, string adminId)
    {
        var (errors, success) = await ValidateAndPersistAsync(rows, default);
        await _db.SaveChangesAsync();

        await _logs.RecordAsync(
            adminId,
            string.Empty,
            "CouponImport",
            "CouponImportJob",
            "-",
            $"total={rows.Count}, ok={success}, errors={errors.Count}",
            null);
        return new CouponImportOutcome(
            CouponImportOutcomeKind.Completed,
            null,
            null,
            null,
            rows.Count,
            success,
            errors);
    }

    private async Task<CouponImportOutcome> SubmitAsync(IFormFile file, string adminId)
    {
        var (job, error) = await _asyncIO.SubmitAsync(adminId, Kind, new XlsxFileValidator(), file);
        if (error is not null || job is null)
        {
            return Error(error ?? "提交导入任务失败。");
        }

        var importJob = new CouponImportJob
        {
            UserId = adminId,
            FileKey = job.FileKey,
            AsyncIOJobId = job.Id,
        };
        _db.Set<CouponImportJob>().Add(importJob);
        await _db.SaveChangesAsync();

        return new CouponImportOutcome(
            CouponImportOutcomeKind.Submitted,
            null,
            null,
            job.Id,
            0,
            0,
            Array.Empty<CouponImportRowError>());
    }

    /// <summary>
    /// Validates every row (format rules then uniqueness), persists the valid
    /// rows as coupons, and returns the collected errors plus success count.
    /// </summary>
    private async Task<(List<CouponImportRowError> Errors, int Success)> ValidateAndPersistAsync(
        IReadOnlyList<CouponImportRow> rows, CancellationToken ct)
    {
        var errors = new List<CouponImportRowError>();
        var valid = new List<CouponImportRow>();

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var rowErrors = ValidateRow(row);
            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                continue;
            }

            valid.Add(row);
        }

        // Within-file duplicate codes: flag every row sharing a code.
        var inFileDuplicates = valid
            .GroupBy(r => NormalizeCode(r.Code))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);
        var deduped = new List<CouponImportRow>();
        foreach (var row in valid)
        {
            if (inFileDuplicates.Contains(NormalizeCode(row.Code)))
            {
                errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "Code", Message = "重复的优惠券代码。" });
            }
            else
            {
                deduped.Add(row);
            }
        }

        // Existing database codes: report, do not overwrite (append-only).
        if (deduped.Count > 0)
        {
            var codes = deduped.Select(r => NormalizeCode(r.Code)).ToList();
            var existing = await _db.Set<Coupon>().AsNoTracking()
                .Where(c => codes.Contains(c.Code))
                .Select(c => c.Code)
                .ToListAsync(ct);
            var existingSet = existing.ToHashSet(StringComparer.Ordinal);
            foreach (var row in deduped)
            {
                if (existingSet.Contains(NormalizeCode(row.Code)))
                {
                    errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "Code", Message = "该优惠券代码已存在。" });
                }
                else
                {
                    _db.Set<Coupon>().Add(new Coupon
                    {
                        Code = NormalizeCode(row.Code),
                        DiscountPercent = row.DiscountType?.Equals("Percent", StringComparison.OrdinalIgnoreCase) == true
                            ? (int)row.DiscountValue!.Value
                            : null,
                        DiscountAmount = row.DiscountType?.Equals("Amount", StringComparison.OrdinalIgnoreCase) == true
                            ? row.DiscountValue
                            : null,
                        ExpiresAt = row.ValidTo,
                        MaxUses = row.MaxRedemptions,
                    });
                }
            }
        }

        return (errors, rows.Count - errors.Count);
    }

    /// <summary>Validates one row per the spec rules.</summary>
    private static List<CouponImportRowError> ValidateRow(CouponImportRow row)
    {
        var errors = new List<CouponImportRowError>();

        if (string.IsNullOrWhiteSpace(row.Code) || !CodeRegex().IsMatch(row.Code))
        {
            errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "Code", Message = "优惠券代码需为 4-32 位字母/数字/下划线/连字符。" });
        }

        var isPercent = row.DiscountType?.Equals("Percent", StringComparison.OrdinalIgnoreCase) == true;
        var isAmount = row.DiscountType?.Equals("Amount", StringComparison.OrdinalIgnoreCase) == true;
        if (!isPercent && !isAmount)
        {
            errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "DiscountType", Message = "折扣类型必须为 Percent 或 Amount。" });
        }

        if (row.DiscountValue is not decimal value || value <= 0 || (isPercent && value > 100))
        {
            errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "DiscountValue", Message = "折扣值必须大于 0（百分比不超过 100）。" });
        }

        var hasRange = row.ValidFrom is not null || row.ValidTo is not null;
        var invalidRange = hasRange
            && (row.ValidFrom is null || row.ValidTo is null || row.ValidFrom.Value >= row.ValidTo.Value);
        if (invalidRange)
        {
            errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "ValidFrom", Message = "有效期起止时间无效（ValidFrom 必须早于 ValidTo）。" });
        }

        if (row.MaxRedemptions is not null && row.MaxRedemptions < 1)
        {
            errors.Add(new CouponImportRowError { RowIndex = row.RowIndex, Field = "MaxRedemptions", Message = "最大使用次数必须大于等于 1。" });
        }

        return errors;
    }

    private static string NormalizeCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private async Task<string?> ValidateUploadAsync(IFormFile file)
    {
        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return "仅支持 .xlsx 文件。";
        }

        var maxBytes = await _config.GetIntAsync("coupon.import.maxBytes", _defaultMaxBytes);
        return file.Length > maxBytes ? $"文件不能超过 {maxBytes / 1024 / 1024} MB。" : null;
    }

    private static List<CouponImportRow> ParseRows(Stream stream)
    {
        var rows = new List<CouponImportRow>();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 2)
        {
            return rows;
        }

        for (var r = 2; r <= lastRow; r++)
        {
            var code = ReadString(sheet, r, 1);
            var discountType = ReadString(sheet, r, 2);
            var discountValue = ReadDecimal(sheet, r, 3);
            var validFrom = ReadDate(sheet, r, 4);
            var validTo = ReadDate(sheet, r, 5);
            var maxRedemptions = ReadInt(sheet, r, 6);

            if (string.IsNullOrWhiteSpace(code)
                && string.IsNullOrWhiteSpace(discountType)
                && discountValue is null
                && validFrom is null
                && validTo is null
                && maxRedemptions is null)
            {
                continue; // blank row
            }

            rows.Add(new CouponImportRow(r, code, discountType, discountValue, validFrom, validTo, maxRedemptions));
        }

        return rows;
    }

    private async Task<string?> WriteErrorFileAsync(int asyncIOJobId, string ownerId, IReadOnlyList<CouponImportRowError> errors)
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
            ownerId, OpenLearning.Storage.Models.FilePurpose.AsyncIO, $"coupon-errors-{asyncIOJobId}.xlsx", "application/octet-stream", stream);
        if (uploadError is null && file is not null)
        {
            return file.Key;
        }

        return null;
    }

    private static string? ReadString(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        return cell.IsEmpty() ? null : cell.GetFormattedString();
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

    private static decimal? ReadDecimal(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return (decimal)cell.GetDouble();
        }

        var text = cell.GetFormattedString();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static DateTime? ReadDate(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime || cell.DataType == XLDataType.Number)
        {
            var value = cell.GetDateTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        var text = cell.GetFormattedString();
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static CouponImportOutcome Error(string message)
    {
        return new CouponImportOutcome(CouponImportOutcomeKind.Error, message, null, null, 0, 0, Array.Empty<CouponImportRowError>());
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
