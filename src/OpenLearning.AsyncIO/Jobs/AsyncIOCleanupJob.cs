using Microsoft.Extensions.Configuration;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Jobs;

namespace OpenLearning.AsyncIO.Jobs;

/// <summary>
/// Prunes result/error files older than `asyncio.retention.days` (default 7)
/// and clears the stored keys. Idempotent.
/// </summary>
public sealed class AsyncIOCleanupJob : IJob
{
    public string Key => "async-io.cleanup";

    public string Cron => "0 2 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly AsyncIOService _service;
    private readonly IConfiguration _config;

    public AsyncIOCleanupJob(AsyncIOService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    public Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var retentionDays = Math.Clamp(_config.GetValue("AsyncIO:RetentionDays", 7), 1, 90);
        return _service.CleanupExpiredAsync(retentionDays);
    }
}
