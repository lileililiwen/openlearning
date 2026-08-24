namespace OpenLearning.Analytics.Models;

/// <summary>
/// Per-course, per-day completion funnel facts: eligible, enrolled, started,
/// and completed counts with defined denominators. Tagged with a refresh run id
/// for atomic serving.
/// </summary>
public class CourseFunnelAggregate
{
    public long Id { get; set; }

    public long RefreshRunId { get; set; }

    public int CourseId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Distinct actors who enrolled (the funnel denominator).</summary>
    public int Eligible { get; set; }

    /// <summary>Distinct actors who enrolled.</summary>
    public int Enrolled { get; set; }

    /// <summary>Distinct actors who started the course.</summary>
    public int Started { get; set; }

    /// <summary>Distinct actors who completed the course.</summary>
    public int Completed { get; set; }
}
