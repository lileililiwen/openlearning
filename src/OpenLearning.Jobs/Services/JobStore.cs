using Microsoft.EntityFrameworkCore;
using OpenLearning.Jobs.Models;

namespace OpenLearning.Jobs.Services;

/// <summary>Persistence for the job registry and run history.</summary>
public class JobStore
{
    private readonly DbContext _db;

    public JobStore(DbContext db)
    {
        _db = db;
    }

    public Task<List<Job>> GetAllAsync()
    {
        return _db.Set<Job>().AsNoTracking().OrderBy(j => j.Key).ToListAsync();
    }

    public Task<Job?> GetByKeyAsync(string key)
    {
        return _db.Set<Job>().AsNoTracking().FirstOrDefaultAsync(j => j.Key == key);
    }

    public Task<Job?> GetByIdAsync(int id)
    {
        return _db.Set<Job>().AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
    }

    public Task<List<JobRun>> GetRunsAsync(int jobId, int count = 30)
    {
        return _db.Set<JobRun>().AsNoTracking()
            .Where(r => r.JobId == jobId)
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    public Task<List<Job>> GetDueAsync(DateTime now)
    {
        return _db.Set<Job>()
            .Where(j => j.IsEnabled && j.NextRunAt <= now)
            .OrderBy(j => j.NextRunAt)
            .ToListAsync();
    }

    /// <summary>Upserts the registry row for an <see cref="IJob"/> registration.</summary>
    public async Task EnsureRegisteredAsync(string key, string cron, DateTime now)
    {
        var job = await _db.Set<Job>().FirstOrDefaultAsync(j => j.Key == key);
        if (job is null)
        {
            _db.Set<Job>().Add(new Job
            {
                Key = key,
                Cron = cron,
                IsEnabled = true,
                LockToken = string.Empty,
                NextRunAt = NextOccurrence(cron, now),
            });
        }
        else
        {
            if (job.Cron != cron)
            {
                job.Cron = cron;
                job.NextRunAt = NextOccurrence(cron, now);
            }

            // Normalize legacy NULL lock tokens to the empty-string sentinel.
            if (string.IsNullOrEmpty(job.LockToken))
            {
                job.LockToken = string.Empty;
            }
        }

        await _db.SaveChangesAsync();
    }

    public Task<bool> HasRunningRunAsync(int jobId)
    {
        return _db.Set<JobRun>().AnyAsync(r => r.JobId == jobId && r.Status == JobRunStatus.Running);
    }

    public Task<bool> HasRunningCycleAsync(string idempotencyKey)
    {
        return _db.Set<JobRun>().AnyAsync(r => r.IdempotencyKey == idempotencyKey && r.Status == JobRunStatus.Running);
    }

    /// <summary>
    /// Atomic compare-and-set on the job's lock token. Returns true when this
    /// caller won the lock. On the InMemory test provider (which has no
    /// relational SQL) the lock is a no-op that always succeeds.
    /// </summary>
    public async Task<bool> TryAcquireLockAsync(int jobId, string expectedToken, string newToken)
    {
        if (!_db.Database.IsRelational())
        {
            return true;
        }

        var affected = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Jobs\" SET \"LockToken\" = {0} WHERE \"Id\" = {1} AND \"LockToken\" = {2}",
            newToken, jobId, expectedToken);
        return affected > 0;
    }

    public Task ReleaseLockAsync(int jobId, string token)
    {
        if (!_db.Database.IsRelational())
        {
            return Task.CompletedTask;
        }

        return _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Jobs\" SET \"LockToken\" = '' WHERE \"Id\" = {0} AND \"LockToken\" = {1}",
            jobId, token);
    }

    public async Task<JobRun> InsertRunAsync(int jobId, string idempotencyKey, string? lockToken, JobRunStatus status = JobRunStatus.Running)
    {
        var run = new JobRun
        {
            JobId = jobId,
            IdempotencyKey = idempotencyKey,
            LockToken = lockToken,
            Status = status,
            StartedAt = DateTime.UtcNow,
        };
        _db.Set<JobRun>().Add(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task CompleteRunAsync(int runId, JobRunStatus status, string? error)
    {
        var run = await _db.Set<JobRun>().FindAsync(runId);
        if (run is null)
        {
            return;
        }

        run.Status = status;
        run.ErrorMessage = error;
        run.FinishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkRanAsync(int jobId, DateTime now)
    {
        var job = await _db.Set<Job>().FindAsync(jobId);
        if (job is null)
        {
            return;
        }

        job.LastRunAt = now;
        job.NextRunAt = NextOccurrence(job.Cron, now);
        await _db.SaveChangesAsync();
    }

    public async Task SetEnabledAsync(int jobId, bool enabled)
    {
        var job = await _db.Set<Job>().FindAsync(jobId);
        if (job is null)
        {
            return;
        }

        job.IsEnabled = enabled;
        if (enabled)
        {
            job.NextRunAt = NextOccurrence(job.Cron, DateTime.UtcNow);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>Marks any run left Running by a crashed process as Failed and releases its lock.</summary>
    public async Task RecoverStaleRunsAsync()
    {
        var stale = await _db.Set<JobRun>().Where(r => r.Status == JobRunStatus.Running).ToListAsync();
        if (stale.Count == 0)
        {
            return;
        }

        foreach (var run in stale)
        {
            run.Status = JobRunStatus.Failed;
            run.ErrorMessage = "Interrupted by a process restart.";
            run.FinishedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(run.LockToken))
            {
                await ReleaseLockAsync(run.JobId, run.LockToken);
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<double> GetSuccessRateAsync(int jobId, DateTime from)
    {
        var total = await _db.Set<JobRun>().CountAsync(r =>
            r.JobId == jobId && r.StartedAt >= from && r.Status != JobRunStatus.Skipped);
        if (total == 0)
        {
            return 0;
        }

        var success = await _db.Set<JobRun>().CountAsync(r =>
            r.JobId == jobId && r.StartedAt >= from && r.Status == JobRunStatus.Success);
        return (double)success / total;
    }

    public async Task<JobRunStatus?> GetLastStatusAsync(int jobId)
    {
        return await _db.Set<JobRun>().Where(r => r.JobId == jobId)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => (JobRunStatus?)r.Status)
            .FirstOrDefaultAsync();
    }

    /// <summary>Next cron occurrence after (or at) the given UTC instant.</summary>
    public static DateTime NextOccurrence(string cron, DateTime fromUtc)
    {
        var expression = ParseCron(cron);
        return expression.GetNextOccurrence(fromUtc, inclusive: true) ?? fromUtc.AddYears(1);
    }

    /// <summary>Smallest window implied by the cron (min gap between consecutive occurrences).</summary>
    public static int CycleSeconds(string cron)
    {
        var expression = ParseCron(cron);
        var now = DateTime.UtcNow;
        var wholeSecond = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        var cursor = expression.GetNextOccurrence(wholeSecond, inclusive: true);
        if (cursor is null)
        {
            return 300;
        }

        var deltas = new List<long>();
        for (var i = 0; i < 8; i++)
        {
            var next = expression.GetNextOccurrence(cursor.Value, inclusive: false);
            if (next is null)
            {
                break;
            }

            deltas.Add((long)(next.Value - cursor.Value).TotalSeconds);
            cursor = next;
        }

        return deltas.Count == 0 ? 300 : Math.Max(1, (int)deltas.Min());
    }

    /// <summary>Parses a 5-field standard cron or a 6-field cron with seconds.</summary>
    public static Cronos.CronExpression ParseCron(string cron)
    {
        var fields = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return fields > 5
            ? Cronos.CronExpression.Parse(cron, Cronos.CronFormat.IncludeSeconds)
            : Cronos.CronExpression.Parse(cron);
    }
}
