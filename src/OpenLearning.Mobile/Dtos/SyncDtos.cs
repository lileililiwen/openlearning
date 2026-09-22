namespace OpenLearning.Mobile.Dtos;

/// <summary>Request to synchronize a lesson-completion mutation.</summary>
public sealed record ProgressSyncRequest(
    string OperationId,
    int CourseId,
    int LessonId,
    int? ContentRevision = null);

/// <summary>Request to synchronize a learner-note upsert.</summary>
public sealed record NoteSyncRequest(
    string OperationId,
    int NoteId,
    int? BaseVersion,
    string Body,
    string ContextType,
    int ContextId,
    int? MediaOffsetSeconds,
    string? Tags);

/// <summary>Canonical outcome returned for an idempotent sync operation.</summary>
public sealed record SyncResult(
    string OperationId,
    string Outcome,
    string? CanonicalState);

/// <summary>
/// Discriminator for items inside a batch sync. The server processes each
/// item independently and returns one <see cref="SyncResult"/> per
/// <c>OperationId</c>; one bad item never fails the whole batch.
/// </summary>
public static class BatchSyncItemTypes
{
    public const string ProgressComplete = "progress.complete";
    public const string NoteUpsert = "note.upsert";
}

/// <summary>
/// One item in a batch sync. The caller fills the fields relevant to
/// <see cref="Type"/>; other fields are ignored.
/// </summary>
public sealed record BatchSyncItem(
    string Type,
    string OperationId,
    int CourseId = 0,
    int LessonId = 0,
    int? ContentRevision = null,
    int? NoteId = null,
    int? BaseVersion = null,
    string? Body = null,
    string? ContextType = null,
    int? ContextId = null,
    int? MediaOffsetSeconds = null,
    string? Tags = null);

/// <summary>Request to synchronize a batch of mutations in a single round trip.</summary>
public sealed record BatchSyncRequest(IReadOnlyList<BatchSyncItem> Items);

/// <summary>Response for a batch sync: one outcome per input item, in input order.</summary>
public sealed record BatchSyncResponse(IReadOnlyList<SyncResult> Results);
