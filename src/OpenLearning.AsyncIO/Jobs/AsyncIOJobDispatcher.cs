using Microsoft.EntityFrameworkCore;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Jobs;
using OpenLearning.Logging.Services;
using OpenLearning.Storage.Services;

namespace OpenLearning.AsyncIO.Jobs;

/// <summary>
/// Polls for pending async IO jobs and dispatches each to the processor
/// registered for its kind. Idempotency: only Pending jobs are processed.
/// </summary>
public sealed class AsyncIOJobDispatcher : IJob
{
    public string Key => "async-io.dispatcher";

    public string Cron => "*/1 * * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly DbContext _db;
    private readonly AsyncIOService _service;
    private readonly StorageService _storage;
    private readonly IEnumerable<IAsyncIOProcessor> _processors;
    private readonly LogService _logs;

    public AsyncIOJobDispatcher(
        DbContext db,
        AsyncIOService service,
        StorageService storage,
        IEnumerable<IAsyncIOProcessor> processors,
        LogService logs)
    {
        _db = db;
        _service = service;
        _storage = storage;
        _processors = processors;
        _logs = logs;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var pending = await _db.Set<AsyncIOJob>()
            .Where(j => j.Status == AsyncIOJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var job in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var processor = _processors.FirstOrDefault(p => p.Kind == job.Kind);
            if (processor is null)
            {
                await _service.FailAsync(job.Id, $"No processor is registered for kind '{job.Kind}'.");
                await AuditAsync(job);
                continue;
            }

            await _service.MarkRunningAsync(job.Id);
            try
            {
                var (file, stream) = await _storage.OpenAsync(job.FileKey);
                if (file is null || stream is null)
                {
                    await _service.FailAsync(job.Id, "The source file is missing.");
                    await AuditAsync(job);
                    continue;
                }

                using (stream)
                {
                    var (ok, error, total, success) = await processor.ProcessAsync(job, stream, cancellationToken);
                    if (ok)
                    {
                        await _service.CompleteAsync(job.Id, total, success, total - success);
                        await AuditAsync(job);
                    }
                    else
                    {
                        await _service.FailAsync(job.Id, error ?? "Processing failed.");
                        await AuditAsync(job);
                    }
                }
            }
            catch (Exception ex)
            {
                await _service.FailAsync(job.Id, ex.Message);
                await AuditAsync(job);
            }
        }
    }

    private async Task AuditAsync(AsyncIOJob job)
    {
        var final = await _db.Set<AsyncIOJob>().AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == job.Id, CancellationToken.None);
        if (final is null)
        {
            return;
        }

        await _logs.RecordAsync(
            null,
            "async-io",
            $"AsyncIO:{final.Kind}:{final.Status}",
            "AsyncIOJob",
            final.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"file={final.FileKey}, rows={final.TotalRows}, ok={final.SuccessRows}, errors={final.ErrorRows}",
            null);
    }
}
