using OpenLearning.Auth.Models;

namespace OpenLearning.Exams.Models;

/// <summary>
/// An authorized reviewer's decision on an incident. Recording is audited and
/// scoped to the course owner; an adverse outcome notifies the learner.
/// </summary>
public class IntegrityDisposition
{
    public int Id { get; set; }

    public int IncidentId { get; set; }

    public IntegrityIncident? Incident { get; set; }

    public string ReviewerId { get; set; } = string.Empty;

    public ApplicationUser? Reviewer { get; set; }

    public IntegrityDispositionOutcome Outcome { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the action was recorded into the audit trail.</summary>
    public DateTime? AuditedAt { get; set; }
}
