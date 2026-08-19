namespace OpenLearning.Jobs;

/// <summary>Context passed to a job execution.</summary>
public sealed record JobContext(int JobId, string IdempotencyKey);

/// <summary>
/// A scheduled business job. Implementations are registered via
/// <c>services.AddJob&lt;T&gt;()</c>; the scheduler upserts the matching
/// <c>Job</c> row and dispatches runs per the cron expression (UTC).
/// </summary>
public interface IJob
{
    /// <summary>Stable unique key for the job; must match across restarts.</summary>
    string Key { get; }

    /// <summary>Cron expression (UTC). Supports 5-field and 6-field (with seconds) forms.</summary>
    string Cron { get; }

    /// <summary>Upper bound for a single run; a run exceeding it is recorded as failed.</summary>
    TimeSpan Timeout { get; }

    Task ExecuteAsync(JobContext context, CancellationToken cancellationToken);
}
