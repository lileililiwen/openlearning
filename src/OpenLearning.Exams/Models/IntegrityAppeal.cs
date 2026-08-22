using OpenLearning.Auth.Models;

namespace OpenLearning.Exams.Models;

/// <summary>
/// A learner's challenge of an adverse disposition. Only the incident's student
/// may submit; a reviewer decides within policy.
/// </summary>
public class IntegrityAppeal
{
    public int Id { get; set; }

    public int IncidentId { get; set; }

    public IntegrityIncident? Incident { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public string Reason { get; set; } = string.Empty;

    public IntegrityAppealStatus Status { get; set; } = IntegrityAppealStatus.Submitted;

    public string? ReviewerId { get; set; }

    public ApplicationUser? Reviewer { get; set; }

    public string? ReviewerNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DecidedAt { get; set; }
}
