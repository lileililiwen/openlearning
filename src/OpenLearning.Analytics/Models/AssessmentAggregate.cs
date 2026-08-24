namespace OpenLearning.Analytics.Models;

/// <summary>
/// Per-assessment performance facts: attempts, completions, average score, and
/// pass rate. Tagged with a refresh run id.
/// </summary>
public class AssessmentAggregate
{
    public long Id { get; set; }

    public long RefreshRunId { get; set; }

    public int AssessmentId { get; set; }

    public int CourseId { get; set; }

    public DateOnly Date { get; set; }

    public int Attempts { get; set; }

    public int Completions { get; set; }

    public double AverageScore { get; set; }

    public double PassRate { get; set; }
}
