namespace OpenLearning.Mobile.Dtos;

/// <summary>Request to synchronize a lesson-completion mutation.</summary>
public sealed record ProgressSyncRequest(
    string OperationId,
    int CourseId,
    int LessonId);

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
