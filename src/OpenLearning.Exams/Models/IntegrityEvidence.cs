namespace OpenLearning.Exams.Models;

/// <summary>
/// One allowlisted integrity event. Stored with the server receive time so
/// client clock manipulation cannot reorder or forge timing. Evidence is
/// never treated as a verdict; it feeds explainable risk scoring only.
/// </summary>
public class IntegrityEvidence
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public IntegritySession? Session { get; set; }

    public int AttemptId { get; set; }

    /// <summary>Monotonic per-session order; must exceed <see cref="IntegritySession.LastSequence"/>.</summary>
    public long Sequence { get; set; }

    /// <summary>Client-supplied batch id used for replay deduplication.</summary>
    public string BatchId { get; set; } = string.Empty;

    public IntegrityEventType EventType { get; set; }

    /// <summary>Allowlisted, non-sensitive payload (e.g. gap seconds). Never audio/video/biometrics.</summary>
    public string? Payload { get; set; }

    public DateTime ClientTimestamp { get; set; }

    /// <summary>Authoritative server time the event was ingested.</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public bool Accepted { get; set; }
}
