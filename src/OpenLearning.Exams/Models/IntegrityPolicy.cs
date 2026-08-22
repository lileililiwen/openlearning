using OpenLearning.Auth.Models;

namespace OpenLearning.Exams.Models;

/// <summary>
/// Versioned, explainable risk policy. A null <see cref="ExamId"/> marks the
/// global default; an exam may override it with its own active policy. Only the
/// active policy of a given scope is used for evaluation.
/// </summary>
public class IntegrityPolicy
{
    public int Id { get; set; }

    public int? ExamId { get; set; }

    public Exam? Exam { get; set; }

    /// <summary>Monotonic version within a scope; higher wins when multiple are active.</summary>
    public int Version { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Risk score at/above which an incident is queued.</summary>
    public int RiskThreshold { get; set; } = 100;

    public int HeartbeatGapWeight { get; set; } = 25;

    public int VisibilityHiddenWeight { get; set; } = 20;

    public int TabSwitchWeight { get; set; } = 15;

    public int CopyAttemptWeight { get; set; } = 15;

    public int PasteAttemptWeight { get; set; } = 10;

    public int ConnectivityLossWeight { get; set; } = 5;

    /// <summary>Days to retain raw evidence before it is purged.</summary>
    public int RetentionDays { get; set; } = 90;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
