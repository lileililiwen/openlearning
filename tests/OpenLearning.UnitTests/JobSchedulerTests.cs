using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Jobs;
using OpenLearning.Jobs.Models;
using OpenLearning.Jobs.Services;
using OpenLearning.Logging.Services;
using Xunit;

namespace OpenLearning.UnitTests.Jobs;

public sealed class JobSchedulerTests
{
    private static (ApplicationDbContext Db, JobStore Store) Seed()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new JobStore(db));
    }

    private sealed class NoopJob : IJob
    {
        public string Key { get; } = "test.noop";

        public string Cron { get; } = "* * * * *";

        public TimeSpan Timeout { get; } = TimeSpan.FromMinutes(1);

        public Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FailingJob : IJob
    {
        public string Key { get; } = "test.failing";

        public string Cron { get; } = "* * * * *";

        public TimeSpan Timeout { get; } = TimeSpan.FromMinutes(1);

        public Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private static JobDispatcher Dispatcher(ApplicationDbContext db, params IJob[] jobs)
    {
        return new JobDispatcher(new JobStore(db), new JobResolver(jobs), new LogService(db));
    }

    [Fact]
    public async Task EnsureRegistered_creates_job_with_next_run()
    {
        var (db, store) = Seed();

        await store.EnsureRegisteredAsync("test.noop", "* * * * *", DateTime.UtcNow);

        var job = Assert.Single(db.Set<Job>());
        Assert.Equal("test.noop", job.Key);
        Assert.True(job.IsEnabled);
        Assert.True(job.NextRunAt > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task GetDue_returns_only_enabled_due_jobs()
    {
        var (db, store) = Seed();
        db.Set<Job>().AddRange(
            new Job { Key = "a", Cron = "* * * * *", IsEnabled = true, NextRunAt = DateTime.UtcNow.AddSeconds(-1) },
            new Job { Key = "b", Cron = "* * * * *", IsEnabled = false, NextRunAt = DateTime.UtcNow.AddSeconds(-1) },
            new Job { Key = "c", Cron = "* * * * *", IsEnabled = true, NextRunAt = DateTime.UtcNow.AddMinutes(5) });
        await db.SaveChangesAsync();

        var due = await store.GetDueAsync(DateTime.UtcNow);

        var key = Assert.Single(due);
        Assert.Equal("a", key.Key);
    }

    [Fact]
    public async Task Dispatch_runs_job_and_records_success()
    {
        var (db, _) = Seed();
        var job = new Job { Key = "test.noop", Cron = "* * * * *", NextRunAt = DateTime.UtcNow };
        db.Set<Job>().Add(job);
        await db.SaveChangesAsync();

        await Dispatcher(db, new NoopJob()).RunDueAsync(job, DateTime.UtcNow, CancellationToken.None);

        var run = Assert.Single(db.Set<JobRun>());
        Assert.Equal(JobRunStatus.Success, run.Status);
        Assert.NotNull(run.FinishedAt);
        Assert.Contains("test.noop:", run.IdempotencyKey);
        var updated = await db.Set<Job>().FindAsync(job.Id);
        Assert.NotNull(updated!.LastRunAt);
    }

    [Fact]
    public async Task Dispatch_records_failure_with_message()
    {
        var (db, _) = Seed();
        var job = new Job { Key = "test.failing", Cron = "* * * * *", NextRunAt = DateTime.UtcNow };
        db.Set<Job>().Add(job);
        await db.SaveChangesAsync();

        await Dispatcher(db, new FailingJob()).RunDueAsync(job, DateTime.UtcNow, CancellationToken.None);

        var run = Assert.Single(db.Set<JobRun>());
        Assert.Equal(JobRunStatus.Failed, run.Status);
        Assert.Contains("boom", run.ErrorMessage!);
    }

    [Fact]
    public async Task Dispatch_skips_when_same_cycle_is_running()
    {
        var (db, _) = Seed();
        var job = new Job { Key = "test.noop", Cron = "* * * * *", NextRunAt = DateTime.UtcNow };
        db.Set<Job>().Add(job);
        await db.SaveChangesAsync();
        var running = new JobRun
        {
            JobId = job.Id,
            IdempotencyKey = $"{job.Key}:123",
            Status = JobRunStatus.Running,
        };
        db.Set<JobRun>().Add(running);
        await db.SaveChangesAsync();

        var dispatcher = Dispatcher(db, new NoopJob());
        await dispatcher.RunDueAsync(job, DateTime.UtcNow, CancellationToken.None);

        var runs = await db.Set<JobRun>().OrderBy(r => r.Id).ToListAsync();
        Assert.Equal(2, runs.Count);
        Assert.Equal(JobRunStatus.Running, runs[0].Status);
        Assert.Equal(JobRunStatus.Skipped, runs[1].Status);
    }

    [Fact]
    public async Task RecoverStaleRuns_marks_running_as_failed_and_releases_lock()
    {
        var (db, store) = Seed();
        var job = new Job { Key = "test.noop", Cron = "* * * * *", LockToken = "stale-token", NextRunAt = DateTime.UtcNow };
        db.Set<Job>().Add(job);
        await db.SaveChangesAsync();
        db.Set<JobRun>().Add(new JobRun { JobId = job.Id, IdempotencyKey = "k", Status = JobRunStatus.Running, LockToken = "stale-token" });
        await db.SaveChangesAsync();

        await store.RecoverStaleRunsAsync();

        var run = Assert.Single(db.Set<JobRun>());
        Assert.Equal(JobRunStatus.Failed, run.Status);
        Assert.Contains("restart", run.ErrorMessage);
    }

    [Fact]
    public async Task Success_rate_counts_non_skipped_runs()
    {
        var (db, store) = Seed();
        var job = new Job { Key = "test.noop", Cron = "* * * * *", NextRunAt = DateTime.UtcNow };
        db.Set<Job>().Add(job);
        await db.SaveChangesAsync();
        db.Set<JobRun>().AddRange(
            new JobRun { JobId = job.Id, IdempotencyKey = "1", Status = JobRunStatus.Success, StartedAt = DateTime.UtcNow.AddDays(-1) },
            new JobRun { JobId = job.Id, IdempotencyKey = "2", Status = JobRunStatus.Failed, StartedAt = DateTime.UtcNow.AddDays(-1) },
            new JobRun { JobId = job.Id, IdempotencyKey = "3", Status = JobRunStatus.Skipped, StartedAt = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();

        var rate = await store.GetSuccessRateAsync(job.Id, DateTime.UtcNow.AddDays(-7));

        Assert.Equal(0.5, rate);
    }

    [Fact]
    public void NextOccurrence_and_cycle_seconds_are_sensible()
    {
        var next = JobStore.NextOccurrence("*/5 * * * *", new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc));
        Assert.Equal(300, JobStore.CycleSeconds("*/5 * * * *"));
        Assert.Equal(new DateTime(2026, 1, 1, 0, 5, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void Six_field_cron_with_seconds_is_supported()
    {
        var next = JobStore.NextOccurrence("* * * * * *", new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc), next);
        Assert.Equal(1, JobStore.CycleSeconds("* * * * * *"));
    }
}
