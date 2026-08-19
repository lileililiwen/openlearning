using Microsoft.EntityFrameworkCore;
using OpenLearning.GradeExport.Models;
using OpenLearning.Jobs;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.GradeExport.Jobs;

/// <summary>
/// Deletes exported files older than <c>grade.export.retentionDays</c> (default
/// 7) and clears the stored key so the download link expires. Idempotent.
/// </summary>
public sealed class GradeExportCleanupJob : IJob
{
    public string Key => "grade.export.cleanup";

    public string Cron => "0 3 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly DbContext _db;
    private readonly StorageService _storage;
    private readonly SystemConfigService _config;

    public GradeExportCleanupJob(DbContext db, StorageService storage, SystemConfigService config)
    {
        _db = db;
        _storage = storage;
        _config = config;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var retentionDays = Math.Clamp(await _config.GetIntAsync("grade.export.retentionDays", 7), 1, 90);
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var jobs = await _db.Set<GradeExportJob>().AsNoTracking()
            .Where(j => j.FileKey != null && j.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            if (job.FileKey is null)
            {
                continue;
            }

            await _storage.DeleteAsync(job.FileKey, job.UserId, isAdmin: true);
            var tracked = await _db.Set<GradeExportJob>().FindAsync(new object[] { job.Id }, cancellationToken);
            if (tracked is not null)
            {
                tracked.FileKey = null;
            }
        }

        if (jobs.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
