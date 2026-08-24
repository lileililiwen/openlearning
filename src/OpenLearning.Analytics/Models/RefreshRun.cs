namespace OpenLearning.Analytics.Models;

/// <summary>Lifecycle state of a scheduled aggregate refresh run.</summary>
public enum RefreshRunStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2,
}

/// <summary>
/// Tracks a scheduled aggregate refresh. Reports only serve facts tagged with
/// the latest <see cref="RefreshRunStatus.Succeeded"/> run so a partial or
/// failed run is never exposed.
/// </summary>
public class RefreshRun
{
    public long Id { get; set; }

    /// <summary>Scope of the run, e.g. "daily".</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>UTC day being aggregated, when the run is date-scoped.</summary>
    public DateOnly? AggregateDate { get; set; }

    public RefreshRunStatus Status { get; set; } = RefreshRunStatus.Running;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? Error { get; set; }
}
