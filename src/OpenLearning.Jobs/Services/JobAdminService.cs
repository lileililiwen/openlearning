using OpenLearning.Jobs.Models;

namespace OpenLearning.Jobs.Services;

/// <summary>A job plus its admin-facing summary fields.</summary>
public sealed record JobSummary(Job Job, JobRunStatus? LastStatus, double SuccessRate);

/// <summary>A job with its recent run history.</summary>
public sealed record JobDetail(Job Job, List<JobRun> Runs);

/// <summary>Admin-facing queries and operations for the job registry.</summary>
public class JobAdminService
{
    private readonly JobStore _store;
    private readonly JobDispatcher _dispatcher;

    public JobAdminService(JobStore store, JobDispatcher dispatcher)
    {
        _store = store;
        _dispatcher = dispatcher;
    }

    public async Task<List<JobSummary>> GetAllAsync()
    {
        var jobs = await _store.GetAllAsync();
        var from = DateTime.UtcNow.AddDays(-7);
        var summaries = new List<JobSummary>(jobs.Count);
        foreach (var job in jobs)
        {
            summaries.Add(new JobSummary(
                job,
                await _store.GetLastStatusAsync(job.Id),
                await _store.GetSuccessRateAsync(job.Id, from)));
        }

        return summaries;
    }

    public async Task<JobDetail?> GetDetailAsync(int jobId)
    {
        var job = await _store.GetByIdAsync(jobId);
        if (job is null)
        {
            return null;
        }

        var runs = await _store.GetRunsAsync(jobId);
        return new JobDetail(job, runs);
    }

    public async Task RunNowAsync(int jobId)
    {
        var job = await _store.GetByIdAsync(jobId);
        if (job is null)
        {
            return;
        }

        await _dispatcher.RunManuallyAsync(job, CancellationToken.None);
    }

    public Task SetEnabledAsync(int jobId, bool enabled)
    {
        return _store.SetEnabledAsync(jobId, enabled);
    }

    public async Task<(bool Ok, string? Error)> UpdateCronAsync(int jobId, string cron)
    {
        var trimmed = (cron ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return (false, "Cron expression is required.");
        }

        try
        {
            JobStore.NextOccurrence(trimmed, DateTime.UtcNow);
        }
        catch (Cronos.CronFormatException)
        {
            return (false, "That is not a valid cron expression.");
        }

        var job = await _store.GetByIdAsync(jobId);
        if (job is null)
        {
            return (false, "Job not found.");
        }

        await _store.EnsureRegisteredAsync(job.Key, trimmed, DateTime.UtcNow);
        return (true, null);
    }
}
