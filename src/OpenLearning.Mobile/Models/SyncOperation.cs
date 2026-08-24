namespace OpenLearning.Mobile.Models;

/// <summary>Kind of mutation a mobile client synchronizes.</summary>
public enum SyncOperationType
{
    ProgressComplete = 0,
    NoteUpsert = 1,
}

/// <summary>Outcome recorded for an idempotent sync operation.</summary>
public enum SyncOutcome
{
    Applied = 0,
    Conflict = 1,
    Rejected = 2,
}

/// <summary>
/// Records the outcome of a client mutation keyed by a client-supplied
/// operation id, so retries return the prior outcome instead of re-applying.
/// </summary>
public class SyncOperation
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Client-supplied unique operation id (idempotency key).</summary>
    public string OperationId { get; set; } = string.Empty;

    public SyncOperationType Type { get; set; }

    public SyncOutcome Outcome { get; set; }

    /// <summary>Canonical state/version returned to the client (JSON or scalar).</summary>
    public string? CanonicalState { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
