namespace OpenLearning.Jobs.Models;

/// <summary>Outcome of a scheduled job run.</summary>
public enum JobRunStatus
{
    Running = 0,
    Success = 1,
    Failed = 2,
    Skipped = 3,
}

/// <summary>One registered scheduled job. Rows are upserted from <c>IJob</c> registrations on startup.</summary>
public class Job
{
    public int Id { get; set; }

    /// <summary>Stable unique key matching the <c>IJob.Key</c> it was registered from.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Cron expression (UTC) evaluated by the scheduler.</summary>
    public string Cron { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastRunAt { get; set; }

    public DateTime NextRunAt { get; set; }

    /// <summary>
    /// Token of the currently running run; empty when no run holds the lock
    /// (a sentinel string keeps the compare-and-set free of NULL parameters).
    /// </summary>
    public string LockToken { get; set; } = string.Empty;
}

/// <summary>One execution attempt of a job.</summary>
public class JobRun
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public Job? Job { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAt { get; set; }

    public JobRunStatus Status { get; set; } = JobRunStatus.Running;

    public string? ErrorMessage { get; set; }

    /// <summary>Derived as <c>Key:cycle</c> so a duplicate tick of the same cycle is detectable.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Lock token held while this run was executing (null for skipped runs).</summary>
    public string? LockToken { get; set; }
}
