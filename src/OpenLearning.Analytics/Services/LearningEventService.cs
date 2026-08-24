using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Analytics.Models;

namespace OpenLearning.Analytics.Services;

/// <summary>Input for a single learning event ingestion.</summary>
public sealed record LearningEventInput(
    string EventType,
    string ActorKey,
    string EventId,
    int? CourseId,
    int? LessonId,
    int? AssessmentId,
    int? ClassGroupId,
    DateTime OccurredAt,
    IReadOnlyDictionary<string, JsonElement>? Properties);

/// <summary>Result of ingesting a learning event.</summary>
public sealed record LearningEventResult(
    bool Accepted,
    bool Duplicate,
    EventValidationOutcome Outcome,
    string? Error);

/// <summary>
/// Ingests allowlisted learning events with deduplication by event identifier
/// and pseudonymous actor keys. Unknown event types are rejected; unknown
/// properties are discarded and the outcome is recorded for operators.
/// </summary>
public class LearningEventService
{
    private readonly DbContext _db;

    public LearningEventService(DbContext db)
    {
        _db = db;
    }

    public async Task<LearningEventResult> IngestAsync(LearningEventInput input)
    {
        if (string.IsNullOrWhiteSpace(input.EventType))
        {
            return new LearningEventResult(false, false, EventValidationOutcome.RejectedUnknownType, "Event type is required.");
        }

        if (string.IsNullOrWhiteSpace(input.ActorKey))
        {
            return new LearningEventResult(false, false, EventValidationOutcome.RejectedUnknownType, "Actor key is required.");
        }

        if (string.IsNullOrWhiteSpace(input.EventId))
        {
            return new LearningEventResult(false, false, EventValidationOutcome.RejectedUnknownType, "Event identifier is required.");
        }

        if (!LearningEventSchema.IsKnownType(input.EventType))
        {
            return new LearningEventResult(false, false, EventValidationOutcome.RejectedUnknownType, "Unknown event type.");
        }

        var duplicate = await _db.Set<LearningEvent>()
            .AnyAsync(e => e.EventId == input.EventId);
        if (duplicate)
        {
            return new LearningEventResult(false, true, EventValidationOutcome.Accepted, null);
        }

        var (propertiesJson, outcome) = FilterProperties(input.EventType, input.Properties);

        _db.Set<LearningEvent>().Add(new LearningEvent
        {
            EventType = input.EventType,
            ActorKey = input.ActorKey,
            EventId = input.EventId,
            CourseId = input.CourseId,
            LessonId = input.LessonId,
            AssessmentId = input.AssessmentId,
            ClassGroupId = input.ClassGroupId,
            OccurredAt = input.OccurredAt,
            ReceivedAt = DateTime.UtcNow,
            SchemaVersion = 1,
            PropertiesJson = propertiesJson,
            ValidationOutcome = outcome,
        });
        await _db.SaveChangesAsync();
        return new LearningEventResult(true, false, outcome, null);
    }

    /// <summary>
    /// Keeps only allowlisted properties for the event type. Returns the JSON
    /// payload and the resulting validation outcome.
    /// </summary>
    private static (string? Json, EventValidationOutcome Outcome) FilterProperties(
        string eventType, IReadOnlyDictionary<string, JsonElement>? properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return (null, EventValidationOutcome.Accepted);
        }

        var allowed = new Dictionary<string, JsonElement>();
        var discarded = false;
        foreach (var (key, value) in properties)
        {
            if (LearningEventSchema.IsAllowedProperty(eventType, key))
            {
                allowed[key] = value;
            }
            else
            {
                discarded = true;
            }
        }

        var json = allowed.Count == 0 ? null : JsonSerializer.Serialize(allowed);
        return (json, discarded ? EventValidationOutcome.DiscardedUnknownProperty : EventValidationOutcome.Accepted);
    }
}
