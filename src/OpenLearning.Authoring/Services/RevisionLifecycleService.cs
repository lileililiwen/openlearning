using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Authoring.Models;

namespace OpenLearning.Authoring.Services;

/// <summary>Outcome of validating a draft (field-level errors, if any).</summary>
public sealed class ValidationOutcome
{
    public bool IsValid { get; init; }

    public List<ValidationError> Errors { get; init; } = new();
}

/// <summary>One field-level validation failure.</summary>
public sealed class ValidationError
{
    public string Field { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

/// <summary>Result of a publish attempt.</summary>
public sealed class PublishResult
{
    public bool Succeeded { get; init; }

    public int? ActiveRevisionId { get; init; }

    public List<ValidationError> Errors { get; init; } = new();
}

/// <summary>Patch applied to a draft revision.</summary>
public sealed class RevisionPatch
{
    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? Level { get; init; }

    public string? Language { get; init; }

    public string? LearningOutcomes { get; init; }

    public string? Prerequisites { get; init; }

    public string? ContentSnapshotJson { get; init; }
}

/// <summary>Thrown when the actor is not the owning instructor.</summary>
public sealed class UnauthorizedRevisionOperationException : Exception
{
    public UnauthorizedRevisionOperationException()
    {
    }

    public UnauthorizedRevisionOperationException(string message)
        : base(message)
    {
    }

    public UnauthorizedRevisionOperationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>Thrown when a revision pointer was mutated concurrently (e.g. two publishes).</summary>
public sealed class RevisionConcurrencyException : Exception
{
    public RevisionConcurrencyException()
    {
    }

    public RevisionConcurrencyException(string message)
        : base(message)
    {
    }

    public RevisionConcurrencyException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Authoring versioning and preview: keeps a mutable draft separate from the
/// active published revision, validates before publishing, and allows owner-only
/// preview/rollback. Learner delivery reads only the active published revision.
/// </summary>
public sealed class RevisionLifecycleService
{
    private readonly DbContext _db;

    public RevisionLifecycleService(DbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the existing draft or creates one (from the active revision if present).</summary>
    public async Task<CourseRevision> EnsureDraftAsync(int courseId, string ownerId)
    {
        var pointer = await GetOrCreatePointerAsync(courseId, ownerId);
        if (pointer.DraftRevisionId is { } draftId)
        {
            var existing = await _db.Set<CourseRevision>().AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == draftId);
            if (existing is not null)
            {
                return existing;
            }
        }

        var draft = await CreateDraftFromAsync(courseId, ownerId, pointer.ActiveRevisionId);
        pointer.DraftRevisionId = draft.Id;
        await SavePointerAsync(pointer);
        return draft;
    }

    /// <summary>Applies an owner-scoped patch to the draft without touching the published revision.</summary>
    public async Task<CourseRevision> EditDraftAsync(int courseId, string actorId, RevisionPatch patch)
    {
        var pointer = await RequireOwnedPointerAsync(courseId, actorId);
        var draft = await GetDraftOrThrowAsync(pointer);
        ApplyPatch(draft, patch);
        await _db.SaveChangesAsync();
        return draft;
    }

    /// <summary>Runs validation, records the result, and returns field-level errors.</summary>
    public async Task<ValidationOutcome> ValidateDraftAsync(int courseId, string actorId)
    {
        var pointer = await RequireOwnedPointerAsync(courseId, actorId);
        var draft = await GetDraftOrThrowAsync(pointer);
        var outcome = Validate(draft);

        _db.Set<RevisionValidationResult>().Add(new RevisionValidationResult
        {
            CourseId = courseId,
            RevisionId = draft.Id,
            CheckedBy = actorId,
            IsValid = outcome.IsValid,
            ErrorsJson = JsonSerializer.Serialize(outcome.Errors),
        });
        await _db.SaveChangesAsync();
        return outcome;
    }

    /// <summary>Owner-only draft preview. Never exposed through catalog or learner delivery.</summary>
    public async Task<CourseRevision> GetPreviewAsync(int courseId, string actorId)
    {
        var pointer = await RequireOwnedPointerAsync(courseId, actorId);
        var draft = await GetDraftOrThrowAsync(pointer);

        _db.Set<RevisionAudit>().Add(new RevisionAudit
        {
            CourseId = courseId,
            RevisionId = draft.Id,
            Action = "Previewed",
            ActorId = actorId,
        });
        await _db.SaveChangesAsync();
        return draft;
    }

    /// <summary>
    /// Atomically promotes the draft to published (rejecting invalid drafts) and
    /// opens a fresh draft for continued editing. Concurrent publishes conflict.
    /// </summary>
    public async Task<PublishResult> PublishAsync(int courseId, string actorId)
    {
        var pointer = await RequireOwnedPointerAsync(courseId, actorId);
        var draft = await GetDraftOrThrowAsync(pointer);

        var outcome = Validate(draft);
        if (!outcome.IsValid)
        {
            return new PublishResult { Succeeded = false, Errors = outcome.Errors };
        }

        var nextNumber = await _db.Set<CourseRevision>()
            .Where(r => r.CourseId == courseId)
            .MaxAsync(r => (int?)r.RevisionNumber) ?? 0;

        var newDraft = new CourseRevision
        {
            CourseId = courseId,
            RevisionNumber = nextNumber + 1,
            State = RevisionState.Draft,
            OwnerId = actorId,
            Title = draft.Title,
            Summary = draft.Summary,
            Level = draft.Level,
            Language = draft.Language,
            LearningOutcomes = draft.LearningOutcomes,
            Prerequisites = draft.Prerequisites,
            ContentSnapshotJson = draft.ContentSnapshotJson,
        };

        draft.State = RevisionState.Published;
        draft.PublishedBy = actorId;
        draft.PublishedAt = DateTime.UtcNow;

        // Persist the promotion and the new draft first so the store-generated
        // ids are available before the pointer is repointed at them.
        _db.Set<CourseRevision>().Update(draft);
        _db.Set<CourseRevision>().Add(newDraft);
        _db.Set<RevisionAudit>().Add(new RevisionAudit
        {
            CourseId = courseId,
            RevisionId = draft.Id,
            Action = "Published",
            ActorId = actorId,
            DetailJson = JsonSerializer.Serialize(new { ActiveRevision = draft.Id, NewDraft = newDraft.Id }),
        });
        await _db.SaveChangesAsync();

        pointer.ActiveRevisionId = draft.Id;
        pointer.DraftRevisionId = newDraft.Id;
        pointer.RowVersion = Guid.NewGuid().ToByteArray();
        _db.Set<CourseRevisionPointer>().Update(pointer);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RevisionConcurrencyException("Another publish modified this course concurrently.", ex);
        }

        return new PublishResult { Succeeded = true, ActiveRevisionId = draft.Id };
    }

    /// <summary>Removes the course from the catalog by clearing the active revision (history unchanged).</summary>
    public async Task UnpublishAsync(int courseId, string actorId)
    {
        var pointer = await RequireOwnedPointerAsync(courseId, actorId);
        if (pointer.ActiveRevisionId is null)
        {
            return;
        }

        var activeId = pointer.ActiveRevisionId.Value;
        pointer.ActiveRevisionId = null;
        _db.Set<RevisionAudit>().Add(new RevisionAudit
        {
            CourseId = courseId,
            RevisionId = activeId,
            Action = "Unpublished",
            ActorId = actorId,
        });
        await SavePointerAsync(pointer);
    }

    /// <summary>Creates a new draft from a prior published revision; existing history is preserved.</summary>
    public async Task<CourseRevision> RollbackAsync(int courseId, string actorId, int targetRevisionId)
    {
        var pointer = await RequireOwnedPointerAsync(courseId, actorId);
        var target = await _db.Set<CourseRevision>()
            .FirstOrDefaultAsync(r => r.Id == targetRevisionId && r.CourseId == courseId);

        if (target is null || target.State != RevisionState.Published)
        {
            throw new InvalidOperationException("Rollback target must be a published revision of this course.");
        }

        var nextNumber = await _db.Set<CourseRevision>()
            .Where(r => r.CourseId == courseId)
            .MaxAsync(r => (int?)r.RevisionNumber) ?? 0;

        var draft = new CourseRevision
        {
            CourseId = courseId,
            RevisionNumber = nextNumber + 1,
            State = RevisionState.Draft,
            OwnerId = actorId,
            Title = target.Title,
            Summary = target.Summary,
            Level = target.Level,
            Language = target.Language,
            LearningOutcomes = target.LearningOutcomes,
            Prerequisites = target.Prerequisites,
            ContentSnapshotJson = target.ContentSnapshotJson,
        };

        _db.Set<CourseRevision>().Add(draft);
        await _db.SaveChangesAsync();

        pointer.DraftRevisionId = draft.Id;
        _db.Set<RevisionAudit>().Add(new RevisionAudit
        {
            CourseId = courseId,
            RevisionId = target.Id,
            Action = "RolledBack",
            ActorId = actorId,
            DetailJson = JsonSerializer.Serialize(new { FromRevision = target.Id, NewDraft = draft.Id }),
        });
        await SavePointerAsync(pointer);
        return draft;
    }

    /// <summary>Learner-safe read: the active published revision only (never the draft).</summary>
    public async Task<CourseRevision?> GetActiveRevisionAsync(int courseId)
    {
        var pointer = await _db.Set<CourseRevisionPointer>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.CourseId == courseId);
        if (pointer?.ActiveRevisionId is not { } activeId)
        {
            return null;
        }

        return await _db.Set<CourseRevision>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == activeId && r.State == RevisionState.Published);
    }

    private async Task<CourseRevisionPointer> GetOrCreatePointerAsync(int courseId, string ownerId)
    {
        var pointer = await _db.Set<CourseRevisionPointer>()
            .FirstOrDefaultAsync(p => p.CourseId == courseId);
        if (pointer is null)
        {
            pointer = new CourseRevisionPointer { CourseId = courseId, OwnerId = ownerId };
            _db.Set<CourseRevisionPointer>().Add(pointer);
            await SavePointerAsync(pointer);
        }

        return pointer;
    }

    private async Task<CourseRevisionPointer> RequireOwnedPointerAsync(int courseId, string actorId)
    {
        var pointer = await _db.Set<CourseRevisionPointer>()
            .FirstOrDefaultAsync(p => p.CourseId == courseId);
        if (pointer is null || pointer.OwnerId != actorId)
        {
            throw new UnauthorizedRevisionOperationException(
                $"Actor '{actorId}' is not authorized to manage revisions for course {courseId}.");
        }

        return pointer;
    }

    private async Task<CourseRevision> GetDraftOrThrowAsync(CourseRevisionPointer pointer)
    {
        if (pointer.DraftRevisionId is not { } draftId)
        {
            throw new InvalidOperationException("No draft revision exists for this course.");
        }

        var draft = await _db.Set<CourseRevision>().FirstOrDefaultAsync(r => r.Id == draftId);
        if (draft is null)
        {
            throw new InvalidOperationException("Draft revision reference is stale.");
        }

        return draft;
    }

    private async Task<CourseRevision> CreateDraftFromAsync(int courseId, string ownerId, int? activeRevisionId)
    {
        var source = activeRevisionId is { } id
            ? (await _db.Set<CourseRevision>().FirstOrDefaultAsync(r => r.Id == id) ?? NewEmpty(courseId, ownerId))
            : NewEmpty(courseId, ownerId);

        var nextNumber = await _db.Set<CourseRevision>()
            .Where(r => r.CourseId == courseId)
            .MaxAsync(r => (int?)r.RevisionNumber) ?? 0;

        var draft = new CourseRevision
        {
            CourseId = courseId,
            RevisionNumber = nextNumber + 1,
            State = RevisionState.Draft,
            OwnerId = ownerId,
            Title = source.Title,
            Summary = source.Summary,
            Level = source.Level,
            Language = source.Language,
            LearningOutcomes = source.LearningOutcomes,
            Prerequisites = source.Prerequisites,
            ContentSnapshotJson = source.ContentSnapshotJson,
        };

        _db.Set<CourseRevision>().Add(draft);
        await _db.SaveChangesAsync();
        return draft;
    }

    private static CourseRevision NewEmpty(int courseId, string ownerId)
    {
        return new CourseRevision
        {
            CourseId = courseId,
            RevisionNumber = 0,
            OwnerId = ownerId,
        };
    }

    private static void ApplyPatch(CourseRevision draft, RevisionPatch patch)
    {
        if (patch.Title is not null)
        {
            draft.Title = patch.Title;
        }

        if (patch.Summary is not null)
        {
            draft.Summary = patch.Summary;
        }

        if (patch.Level is not null)
        {
            draft.Level = patch.Level;
        }

        if (patch.Language is not null)
        {
            draft.Language = patch.Language;
        }

        if (patch.LearningOutcomes is not null)
        {
            draft.LearningOutcomes = patch.LearningOutcomes;
        }

        if (patch.Prerequisites is not null)
        {
            draft.Prerequisites = patch.Prerequisites;
        }

        if (patch.ContentSnapshotJson is not null)
        {
            draft.ContentSnapshotJson = patch.ContentSnapshotJson;
        }
    }

    private static ValidationOutcome Validate(CourseRevision draft)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            errors.Add(new ValidationError { Field = nameof(draft.Title), Code = "Required", Message = "Title is required." });
        }

        if (string.IsNullOrWhiteSpace(draft.Summary))
        {
            errors.Add(new ValidationError { Field = nameof(draft.Summary), Code = "Required", Message = "Summary is required." });
        }

        if (string.IsNullOrWhiteSpace(draft.Language))
        {
            errors.Add(new ValidationError { Field = nameof(draft.Language), Code = "Required", Message = "Language is required." });
        }

        if (string.IsNullOrWhiteSpace(draft.Level))
        {
            errors.Add(new ValidationError { Field = nameof(draft.Level), Code = "Required", Message = "Level is required." });
        }

        if (!string.IsNullOrWhiteSpace(draft.ContentSnapshotJson))
        {
            ContentSnapshot? snapshot = null;
            try
            {
                snapshot = JsonSerializer.Deserialize<ContentSnapshot>(draft.ContentSnapshotJson);
            }
            catch (JsonException)
            {
                errors.Add(new ValidationError
                {
                    Field = nameof(draft.ContentSnapshotJson),
                    Code = "InvalidJson",
                    Message = "Content snapshot is not valid JSON.",
                });
            }

            if (snapshot is not null)
            {
                if (snapshot.Modules.Count == 0)
                {
                    errors.Add(new ValidationError
                    {
                        Field = nameof(draft.ContentSnapshotJson),
                        Code = "EmptyOutline",
                        Message = "Course must contain at least one module.",
                    });
                }
                else
                {
                    for (var i = 0; i < snapshot.Modules.Count; i++)
                    {
                        var module = snapshot.Modules[i];
                        if (string.IsNullOrWhiteSpace(module.Title))
                        {
                            errors.Add(new ValidationError
                            {
                                Field = $"{nameof(draft.ContentSnapshotJson)}.Modules[{i}].Title",
                                Code = "Required",
                                Message = $"Module {i + 1} requires a title.",
                            });
                        }

                        if (module.Lessons.Count == 0)
                        {
                            errors.Add(new ValidationError
                            {
                                Field = $"{nameof(draft.ContentSnapshotJson)}.Modules[{i}].Lessons",
                                Code = "Required",
                                Message = $"Module '{module.Title}' must contain at least one lesson.",
                            });
                        }

                        for (var j = 0; j < module.Lessons.Count; j++)
                        {
                            if (string.IsNullOrWhiteSpace(module.Lessons[j].Title))
                            {
                                errors.Add(new ValidationError
                                {
                                    Field = $"{nameof(draft.ContentSnapshotJson)}.Modules[{i}].Lessons[{j}].Title",
                                    Code = "Required",
                                    Message = $"Lesson {j + 1} in module {i + 1} requires a title.",
                                });
                            }
                        }
                    }
                }
            }
        }

        return new ValidationOutcome { IsValid = errors.Count == 0, Errors = errors };
    }

    private async Task SavePointerAsync(CourseRevisionPointer pointer)
    {
        pointer.RowVersion = Guid.NewGuid().ToByteArray();
        if (_db.Entry(pointer).State == EntityState.Detached)
        {
            _db.Set<CourseRevisionPointer>().Add(pointer);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RevisionConcurrencyException("The course revision pointer was modified concurrently.", ex);
        }
    }
}
