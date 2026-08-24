namespace OpenLearning.Analytics.Models;

/// <summary>
/// Per-course, per-cohort retention facts: how many distinct actors from a
/// cohort remained active in a given period after enrollment. Tagged with a
/// refresh run id.
/// </summary>
public class CohortRetentionAggregate
{
    public long Id { get; set; }

    public long RefreshRunId { get; set; }

    public int CourseId { get; set; }

    public int ClassGroupId { get; set; }

    /// <summary>Days since cohort enrollment (0 = enrollment period).</summary>
    public int PeriodIndex { get; set; }

    /// <summary>Distinct actors in the cohort still active in this period.</summary>
    public int Retained { get; set; }
}
