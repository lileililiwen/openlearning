namespace OpenLearning.Analytics.Models;

/// <summary>
/// Per-course, per-day teaching workload facts: scheduled teaching hours and a
/// grading-workload proxy. Tagged with a refresh run id.
/// </summary>
public class WorkloadAggregate
{
    public long Id { get; set; }

    public long RefreshRunId { get; set; }

    public int CourseId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Scheduled live teaching hours that day.</summary>
    public double TeachingHours { get; set; }

    /// <summary>Grading-workload proxy (completed assessments that day).</summary>
    public int GradingWorkload { get; set; }
}
