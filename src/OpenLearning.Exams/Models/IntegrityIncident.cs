namespace OpenLearning.Exams.Models;

/// <summary>
/// A queued integrity concern. Created only by risk evaluation; never alters a
/// grade automatically. <see cref="ContributingRules"/> explains which policy
/// rules contributed, keeping the score explainable and versioned.
/// </summary>
public class IntegrityIncident
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public ExamAttempt? Attempt { get; set; }

    public int ExamId { get; set; }

    public int CourseId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public IntegrityRiskLevel RiskLevel { get; set; }

    public int RiskScore { get; set; }

    /// <summary>JSON array of {rule, weight, count} objects explaining the score.</summary>
    public string ContributingRules { get; set; } = "[]";

    public int PolicyVersion { get; set; }

    public IntegrityIncidentStatus Status { get; set; } = IntegrityIncidentStatus.Open;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public ICollection<IntegrityDisposition> Dispositions { get; set; } = new List<IntegrityDisposition>();

    public ICollection<IntegrityAppeal> Appeals { get; set; } = new List<IntegrityAppeal>();
}
