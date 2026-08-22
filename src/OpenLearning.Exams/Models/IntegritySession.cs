using OpenLearning.Auth.Models;

namespace OpenLearning.Exams.Models;

/// <summary>
/// Server-authoritative integrity session bound to one attempt. The signed
/// <see cref="Nonce"/> lets the client prove the session is genuine, while
/// <see cref="ExpiresAt"/> is always server time so client clock changes
/// cannot extend the deadline.
/// </summary>
public class IntegritySession
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public ExamAttempt? Attempt { get; set; }

    /// <summary>Opaque, signed attempt token returned to the client.</summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 signature over the nonce + attempt id.</summary>
    public string Signature { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Server-controlled deadline (never trusts client clocks).</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public IntegritySessionStatus Status { get; set; } = IntegritySessionStatus.Active;

    /// <summary>Highest accepted monotonic sequence; gaps/replays are rejected.</summary>
    public long LastSequence { get; set; }

    public DateTime? LastEventAt { get; set; }

    public ICollection<IntegrityEvidence> Evidence { get; set; } = new List<IntegrityEvidence>();
}
