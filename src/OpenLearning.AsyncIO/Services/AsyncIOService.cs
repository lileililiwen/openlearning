using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenLearning.AsyncIO.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.AsyncIO.Services;

/// <summary>Validates an upload for a specific IO consumer before storage.</summary>
public interface IIOFileValidator
{
    string[] AllowedExtensions { get; }

    long MaxBytes { get; }

    /// <summary>Returns null when acceptable, else a rejection message.</summary>
    string? Validate(IFormFile file);
}

/// <summary>Consumes one async IO job (import/export). Implemented per consumer kind.</summary>
public interface IAsyncIOProcessor
{
    string Kind { get; }

    Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream fileStream, CancellationToken cancellationToken);
}

/// <summary>
/// Persists async IO jobs, manages their lifecycle, stores result/error files,
/// and notifies the owner.
/// </summary>
public class AsyncIOService
{
    private readonly DbContext _db;
    private readonly StorageService _storage;
    private readonly NotificationService _notifications;

    public AsyncIOService(DbContext db, StorageService storage, NotificationService notifications)
    {
        _db = db;
        _storage = storage;
        _notifications = notifications;
    }

    public async Task<(AsyncIOJob? Job, string? Error)> SubmitAsync(
        string ownerId, string kind, IIOFileValidator validator, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "No file was uploaded.");
        }

        var validationError = validator?.Validate(file);
        if (validationError is not null)
        {
            return (null, validationError);
        }

        var (stored, uploadError) = await _storage.UploadAsync(
            ownerId, FilePurpose.AsyncIO, file.FileName, file.ContentType, file.OpenReadStream());
        if (uploadError is not null || stored is null)
        {
            return (null, uploadError ?? "Failed to store the file.");
        }

        var job = new AsyncIOJob
        {
            UserId = ownerId,
            Kind = kind,
            FileKey = stored.Key,
        };
        _db.Set<AsyncIOJob>().Add(job);
        await _db.SaveChangesAsync();
        return (job, null);
    }

    public Task<AsyncIOJob?> GetJobAsync(int id, string ownerId, bool isAdmin)
    {
        return _db.Set<AsyncIOJob>().AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id && (isAdmin || j.UserId == ownerId));
    }

    public Task<List<AsyncIOJob>> ListJobsAsync(string? ownerId, bool isAdmin, string? kind = null, AsyncIOJobStatus? status = null, int page = 1, int pageSize = 30)
    {
        IQueryable<AsyncIOJob> query = _db.Set<AsyncIOJob>().AsNoTracking();
        if (!isAdmin)
        {
            query = query.Where(j => j.UserId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            query = query.Where(j => j.Kind == kind);
        }

        if (status is not null)
        {
            query = query.Where(j => j.Status == status);
        }

        return query.OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task MarkRunningAsync(int jobId)
    {
        var job = await _db.Set<AsyncIOJob>().FindAsync(jobId);
        if (job is null || job.Status != AsyncIOJobStatus.Pending)
        {
            return;
        }

        job.Status = AsyncIOJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task CompleteAsync(int jobId, int totalRows, int successRows, int errorRows)
    {
        var job = await _db.Set<AsyncIOJob>().FindAsync(jobId);
        if (job is null)
        {
            return;
        }

        job.Status = AsyncIOJobStatus.Success;
        job.FinishedAt = DateTime.UtcNow;
        job.TotalRows = totalRows;
        job.SuccessRows = successRows;
        job.ErrorRows = errorRows;
        await _db.SaveChangesAsync();

        if (job.ResultFileKey is not null)
        {
            // Export jobs carry a result file; notify with the download link and expiry.
            await _notifications.SendAsync(
                NotificationService.EventKeys.ExportReady,
                job.UserId,
                new Dictionary<string, string>
                {
                    ["kind"] = job.Kind,
                    ["downloadUrl"] = $"/files/{job.ResultFileKey}",
                    ["expiresAt"] = job.CreatedAt.AddDays(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                },
                $"/files/{job.ResultFileKey}");
        }
        else
        {
            // Import jobs notify with the success/error counts and error-file link.
            await _notifications.SendAsync(
                NotificationService.EventKeys.ImportCompleted,
                job.UserId,
                new Dictionary<string, string>
                {
                    ["kind"] = job.Kind,
                    ["successCount"] = successRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["errorCount"] = errorRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["errorFileLink"] = job.ErrorFileKey is null ? string.Empty : $"/files/{job.ErrorFileKey}",
                },
                $"/Admin/AsyncIO/Index");
        }
    }

    public async Task FailAsync(int jobId, string message)
    {
        var job = await _db.Set<AsyncIOJob>().FindAsync(jobId);
        if (job is null)
        {
            return;
        }

        job.Status = AsyncIOJobStatus.Failed;
        job.FinishedAt = DateTime.UtcNow;
        job.ErrorMessage = (message ?? string.Empty)[..Math.Min(message?.Length ?? 0, 2000)];
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(
            NotificationService.EventKeys.ImportFailed,
            job.UserId,
            new Dictionary<string, string>
            {
                ["kind"] = job.Kind,
                ["error"] = job.ErrorMessage ?? string.Empty,
            });
    }

    /// <summary>Stores the result file (export output) and sets its key.</summary>
    public async Task SetResultAsync(int jobId, string fileName, Stream content)
    {
        var job = await _db.Set<AsyncIOJob>().FindAsync(jobId);
        if (job is null)
        {
            return;
        }

        var (file, error) = await _storage.UploadAsync(
            job.UserId, FilePurpose.AsyncIO, fileName, "application/octet-stream", content);
        if (error is null && file is not null)
        {
            job.ResultFileKey = file.Key;
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Writes the row-error file for a failed import and returns its key.</summary>
    public async Task<string?> WriteErrorFileAsync(int jobId, IEnumerable<AsyncIORowError> errors)
    {
        var errorList = errors.ToList();
        if (errorList.Count == 0)
        {
            return null;
        }

        var job = await _db.Set<AsyncIOJob>().FindAsync(jobId);
        if (job is null)
        {
            return null;
        }

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Row,Field,Message");
        foreach (var error in errorList)
        {
            csv.AppendLine(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2}",
                error.RowIndex,
                CsvEscape(error.Field),
                CsvEscape(error.Message)));
        }

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv.ToString()));
        var (file, uploadError) = await _storage.UploadAsync(
            job.UserId, FilePurpose.AsyncIO, $"errors-{job.Id}.csv", "text/csv", stream);
        if (uploadError is null && file is not null)
        {
            job.ErrorFileKey = file.Key;
            await _db.SaveChangesAsync();
            return file.Key;
        }

        return null;
    }

    /// <summary>Emits an export.progress notification at the given percentage (25/50/75).</summary>
    public async Task ReportProgressAsync(int jobId, int percent)
    {
        var job = await _db.Set<AsyncIOJob>().AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        var clamped = Math.Clamp(percent, 0, 100);
        await _notifications.SendAsync(
            NotificationService.EventKeys.ExportProgress,
            job.UserId,
            new Dictionary<string, string>
            {
                ["kind"] = job.Kind,
                ["percent"] = clamped.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
    }

    /// <summary>Prunes result/error files older than the retention period. Returns the count pruned.</summary>
    public async Task<int> CleanupExpiredAsync(int retentionDays, Func<string, Task>? deleteFile = null)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var jobs = await _db.Set<AsyncIOJob>().AsNoTracking()
            .Where(j => (j.ResultFileKey != null || j.ErrorFileKey != null) && j.CreatedAt < cutoff)
            .ToListAsync();

        foreach (var job in jobs)
        {
            if (job.ResultFileKey is not null)
            {
                if (deleteFile is not null)
                {
                    await deleteFile(job.ResultFileKey);
                }
                else
                {
                    await _storage.DeleteAsync(job.ResultFileKey, job.UserId, isAdmin: true);
                }
            }

            if (job.ErrorFileKey is not null)
            {
                if (deleteFile is not null)
                {
                    await deleteFile(job.ErrorFileKey);
                }
                else
                {
                    await _storage.DeleteAsync(job.ErrorFileKey, job.UserId, isAdmin: true);
                }
            }

            var tracked = await _db.Set<AsyncIOJob>().FindAsync(job.Id);
            if (tracked is not null)
            {
                tracked.ResultFileKey = null;
                tracked.ErrorFileKey = null;
            }
        }

        if (jobs.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return jobs.Count;
    }

    private static string CsvEscape(string value)
    {
        var text = (value ?? string.Empty).Replace("\"", "\"\"");
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text}\""
            : text;
    }
}
