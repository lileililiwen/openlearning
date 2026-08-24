using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Mobile.Dtos;
using OpenLearning.Mobile.Models;
using OpenLearning.Progress.Services;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;

namespace OpenLearning.Mobile.Services;

/// <summary>
/// Idempotent synchronization of offline mutations. Each client mutation
/// carries an operation id; retries return the prior outcome. Lesson
/// completion is monotonic (recorded once), while editable learner notes use
/// server versions and report explicit conflicts with canonical state.
/// </summary>
public class MobileSyncService
{
    private readonly DbContext _db;
    private readonly ProgressService _progress;
    private readonly LearnerNoteService _notes;

    public MobileSyncService(DbContext db, ProgressService progress, LearnerNoteService notes)
    {
        _db = db;
        _progress = progress;
        _notes = notes;
    }

    /// <summary>
    /// Synchronizes a lesson-completion mutation. Idempotent: a retried
    /// operation id returns the prior outcome without re-applying.
    /// </summary>
    public async Task<SyncResult> SyncProgressAsync(string userId, ProgressSyncRequest request)
    {
        var prior = await _db.Set<SyncOperation>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OperationId == request.OperationId);
        if (prior is not null)
        {
            return new SyncResult(request.OperationId, prior.Outcome.ToString().ToLowerInvariant(), prior.CanonicalState);
        }

        var (ok, error) = await _progress.MarkCompleteAsync(userId, request.CourseId, request.LessonId);
        var outcome = ok ? SyncOutcome.Applied : SyncOutcome.Rejected;
        var canonical = ok ? "{\"completed\":true}" : JsonSerializer.Serialize(new { error });

        _db.Set<SyncOperation>().Add(new SyncOperation
        {
            UserId = userId,
            OperationId = request.OperationId,
            Type = SyncOperationType.ProgressComplete,
            Outcome = outcome,
            CanonicalState = canonical,
        });
        await _db.SaveChangesAsync();

        return new SyncResult(request.OperationId, outcome.ToString().ToLowerInvariant(), canonical);
    }

    /// <summary>
    /// Synchronizes a learner-note upsert. Uses the server note version for
    /// conflict detection: a stale base version returns a conflict with the
    /// canonical server state instead of overwriting.
    /// </summary>
    public async Task<SyncResult> SyncNoteAsync(string userId, NoteSyncRequest request)
    {
        var prior = await _db.Set<SyncOperation>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OperationId == request.OperationId);
        if (prior is not null)
        {
            return new SyncResult(request.OperationId, prior.Outcome.ToString().ToLowerInvariant(), prior.CanonicalState);
        }

        var existing = await _notes.GetByIdAsync(userId, request.NoteId);
        if (existing is not null && request.BaseVersion is int baseVersion && existing.UpdatedAt.Ticks != baseVersion)
        {
            var conflictState = JsonSerializer.Serialize(new
            {
                noteId = existing.Id,
                version = existing.UpdatedAt.Ticks,
                body = existing.Body,
                contextType = existing.ContextType.ToString(),
                contextId = existing.ContextId,
                mediaOffsetSeconds = existing.MediaOffsetSeconds,
                tags = existing.Tags,
            });
            _db.Set<SyncOperation>().Add(new SyncOperation
            {
                UserId = userId,
                OperationId = request.OperationId,
                Type = SyncOperationType.NoteUpsert,
                Outcome = SyncOutcome.Conflict,
                CanonicalState = conflictState,
            });
            await _db.SaveChangesAsync();
            return new SyncResult(request.OperationId, "conflict", conflictState);
        }

        var contextType = Enum.TryParse<NoteContextType>(request.ContextType, true, out var parsed)
            ? parsed
            : NoteContextType.Course;
        var input = new NoteInput(
            request.Body, contextType, request.ContextId, request.MediaOffsetSeconds, request.Tags);

        string canonical;
        if (existing is null)
        {
            var (id, error) = await _notes.CreateAsync(userId, input);
            if (id == 0)
            {
                canonical = JsonSerializer.Serialize(new { error });
                _db.Set<SyncOperation>().Add(new SyncOperation
                {
                    UserId = userId,
                    OperationId = request.OperationId,
                    Type = SyncOperationType.NoteUpsert,
                    Outcome = SyncOutcome.Rejected,
                    CanonicalState = canonical,
                });
                await _db.SaveChangesAsync();
                return new SyncResult(request.OperationId, "rejected", canonical);
            }

            var created = await _notes.GetByIdAsync(userId, id);
            canonical = JsonSerializer.Serialize(new
            {
                noteId = created!.Id,
                version = created.UpdatedAt.Ticks,
                body = created.Body,
                contextType = created.ContextType.ToString(),
                contextId = created.ContextId,
                mediaOffsetSeconds = created.MediaOffsetSeconds,
                tags = created.Tags,
            });
        }
        else
        {
            var (ok, error) = await _notes.UpdateAsync(userId, request.NoteId, input);
            if (!ok)
            {
                canonical = JsonSerializer.Serialize(new { error });
                _db.Set<SyncOperation>().Add(new SyncOperation
                {
                    UserId = userId,
                    OperationId = request.OperationId,
                    Type = SyncOperationType.NoteUpsert,
                    Outcome = SyncOutcome.Rejected,
                    CanonicalState = canonical,
                });
                await _db.SaveChangesAsync();
                return new SyncResult(request.OperationId, "rejected", canonical);
            }

            var updated = await _notes.GetByIdAsync(userId, request.NoteId);
            canonical = JsonSerializer.Serialize(new
            {
                noteId = updated!.Id,
                version = updated.UpdatedAt.Ticks,
                body = updated.Body,
                contextType = updated.ContextType.ToString(),
                contextId = updated.ContextId,
                mediaOffsetSeconds = updated.MediaOffsetSeconds,
                tags = updated.Tags,
            });
        }

        _db.Set<SyncOperation>().Add(new SyncOperation
        {
            UserId = userId,
            OperationId = request.OperationId,
            Type = SyncOperationType.NoteUpsert,
            Outcome = SyncOutcome.Applied,
            CanonicalState = canonical,
        });
        await _db.SaveChangesAsync();

        return new SyncResult(request.OperationId, "applied", canonical);
    }
}
