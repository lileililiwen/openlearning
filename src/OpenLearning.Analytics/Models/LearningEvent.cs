namespace OpenLearning.Analytics.Models;

/// <summary>Outcome of validating an incoming learning event against its allowlisted schema.</summary>
public enum EventValidationOutcome
{
    /// <summary>Event accepted and stored for aggregation.</summary>
    Accepted = 0,

    /// <summary>Event accepted but one or more unknown properties were discarded.</summary>
    DiscardedUnknownProperty = 1,

    /// <summary>Event rejected because its type is not allowlisted.</summary>
    RejectedUnknownType = 2,
}

/// <summary>
/// A versioned, allowlisted learning event. The actor is stored as a
/// pseudonymous key (never a raw identity), the event identifier is unique for
/// deduplication, and only allowlisted properties are retained as JSON.
/// </summary>
public class LearningEvent
{
    public long Id { get; set; }

    /// <summary>Allowlisted event type, e.g. "course.started".</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Pseudonymous actor key; never a raw user identity.</summary>
    public string ActorKey { get; set; } = string.Empty;

    /// <summary>Client-supplied unique identifier used to deduplicate repeats.</summary>
    public string EventId { get; set; } = string.Empty;

    public int? CourseId { get; set; }

    public int? LessonId { get; set; }

    public int? AssessmentId { get; set; }

    /// <summary>Cohort / term the event belongs to, when known.</summary>
    public int? ClassGroupId { get; set; }

    /// <summary>When the event occurred at the source.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>When the event was received by the platform.</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Schema version of the event envelope.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Allowlisted properties serialized as JSON.</summary>
    public string? PropertiesJson { get; set; }

    /// <summary>Validation outcome, observable to operators.</summary>
    public EventValidationOutcome ValidationOutcome { get; set; } = EventValidationOutcome.Accepted;
}
