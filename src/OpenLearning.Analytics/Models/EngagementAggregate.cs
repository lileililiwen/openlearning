namespace OpenLearning.Analytics.Models;

/// <summary>
/// Per-course, per-day active-learning engagement facts: distinct active
/// learners and accumulated active seconds. Tagged with a refresh run id.
/// </summary>
public class EngagementAggregate
{
    public long Id { get; set; }

    public long RefreshRunId { get; set; }

    public int CourseId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Distinct actors with any learning activity that day.</summary>
    public int ActiveLearners { get; set; }

    /// <summary>Accumulated active learning seconds that day.</summary>
    public long ActiveSeconds { get; set; }
}
