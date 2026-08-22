namespace OpenLearning.Exams.Models;

/// <summary>
/// Audit record of authorized reviewer access to integrity evidence and
/// decisions. Written for every review action so access is traceable.
/// </summary>
public class IntegrityAccessLog
{
    public int Id { get; set; }

    public int? IncidentId { get; set; }

    public int? SessionId { get; set; }

    public string ReviewerId { get; set; } = string.Empty;

    public IntegrityAccessAction Action { get; set; }

    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}
