namespace OpenLearning.Analytics.Models;

/// <summary>
/// Configurable analytics retention and privacy controls. A single row is
/// seeded with the default policy; operators may adjust it.
/// </summary>
public class RetentionPolicy
{
    public int Id { get; set; }

    /// <summary>Stable key, e.g. "learning-events".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>How long raw learning events are retained before pruning.</summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>Segments below this size are suppressed from reports and exports.</summary>
    public int CohortThreshold { get; set; } = 5;
}
