using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Jobs.Models;
using OpenLearning.Logging.Services;

namespace OpenLearning.Jobs.Services;

/// <summary>
/// Executes a due job under an idempotency key and a per-job lock, records
/// the run outcome, updates the next-run schedule, and writes an operation
/// log entry for every outcome.
/// </summary>
public class JobDispatcher
{
    private readonly JobStore _store;
    private readonly JobResolver _resolver;
    private readonly LogService _logs;

    public JobDispatcher(JobStore store, JobResolver resolver, LogService logs)
    {
        _store = store;
        _resolver = resolver;
        _logs = logs;
    }

    /// <summary>Runs a job whose next-run time has arrived, skipping duplicate or overlapping cycles.</summary>
    public async Task RunDueAsync(Job job, DateTime now, CancellationToken cancellationToken)
    {
        var cycle = JobStore.CycleSeconds(job.Cron);
        var cycleIndex = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / cycle;
        var idempotencyKey = $"{job.Key}:{cycleIndex}";

        var sameCycleRunning = await _store.HasRunningCycleAsync(idempotencyKey);
        var anyRunning = await _store.HasRunningRunAsync(job.Id);
        if (sameCycleRunning || anyRunning)
        {
            await _store.InsertRunAsync(job.Id, idempotencyKey, null, JobRunStatus.Skipped);
            await _logs.RecordAsync(
                null,
                "scheduler",
                "JobSkipped",
                "Job",
                job.Id.ToString(CultureInfo.InvariantCulture),
                $"Duplicate or overlapping cycle skipped: {idempotencyKey}",
                null);
            return;
        }

        await ExecuteCoreAsync(job, idempotencyKey, cancellationToken);
    }

    /// <summary>Manually triggered run from the admin UI; bypasses the cron cycle checks.</summary>
    public async Task RunManuallyAsync(Job job, CancellationToken cancellationToken)
    {
        var idempotencyKey = $"{job.Key}:manual:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        await ExecuteCoreAsync(job, idempotencyKey, cancellationToken);
    }

    private async Task ExecuteCoreAsync(Job job, string idempotencyKey, CancellationToken cancellationToken)
    {
        var lockToken = Guid.NewGuid().ToString("N");
        var expectedToken = string.IsNullOrEmpty(job.LockToken) ? string.Empty : job.LockToken;
        if (!await _store.TryAcquireLockAsync(job.Id, expectedToken, lockToken))
        {
            await _store.InsertRunAsync(job.Id, idempotencyKey, null, JobRunStatus.Skipped);
            return;
        }

        var run = await _store.InsertRunAsync(job.Id, idempotencyKey, lockToken);
        try
        {
            var executor = _resolver.Resolve(job.Key);
            if (executor is null)
            {
                throw new InvalidOperationException($"No IJob is registered for key '{job.Key}'.");
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(executor.Timeout);
            await executor.ExecuteAsync(new JobContext(job.Id, idempotencyKey), timeoutCts.Token);

            await _store.CompleteRunAsync(run.Id, JobRunStatus.Success, null);
            await _store.MarkRanAsync(job.Id, DateTime.UtcNow);
            await _store.ReleaseLockAsync(job.Id, lockToken);
            await _logs.RecordAsync(
                null,
                "scheduler",
                $"JobSuccess:{job.Key}",
                "Job",
                job.Id.ToString(CultureInfo.InvariantCulture),
                idempotencyKey,
                null);
        }
        catch (Exception ex)
        {
            var message = Truncate(ex.Message);
            await _store.CompleteRunAsync(run.Id, JobRunStatus.Failed, message);
            await _store.MarkRanAsync(job.Id, DateTime.UtcNow);
            await _store.ReleaseLockAsync(job.Id, lockToken);
            await _logs.RecordAsync(
                null,
                "scheduler",
                $"JobFailed:{job.Key}",
                "Job",
                job.Id.ToString(CultureInfo.InvariantCulture),
                message,
                null);
        }
    }

    private static string Truncate(string? value, int max = 1000)
    {
        var text = value ?? string.Empty;
        return text.Length <= max ? text : text[..max];
    }
}
