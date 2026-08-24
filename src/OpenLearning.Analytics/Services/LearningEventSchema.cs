namespace OpenLearning.Analytics.Services;

/// <summary>
/// Static registry of allowlisted learning event types and the properties each
/// may carry. Any property outside this registry is discarded (or the event
/// rejected if its type is unknown), per the allowlisted-envelope design.
/// </summary>
public static class LearningEventSchema
{
    public const string CourseEnrolled = "course.enrolled";
    public const string CourseStarted = "course.started";
    public const string CourseCompleted = "course.completed";
    public const string LessonCompleted = "lesson.completed";
    public const string AssessmentAttempted = "assessment.attempted";
    public const string AssessmentCompleted = "assessment.completed";
    public const string LiveAttended = "live.attended";
    public const string SessionActive = "session.active";

    /// <summary>Allowlisted property names per event type.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedProperties =
        new Dictionary<string, IReadOnlySet<string>>
        {
            [CourseEnrolled] = new HashSet<string>(),
            [CourseStarted] = new HashSet<string>(),
            [CourseCompleted] = new HashSet<string>(),
            [LessonCompleted] = new HashSet<string>(),
            [AssessmentAttempted] = new HashSet<string> { "score", "maxScore" },
            [AssessmentCompleted] = new HashSet<string> { "score", "maxScore", "passed" },
            [LiveAttended] = new HashSet<string> { "hours" },
            [SessionActive] = new HashSet<string> { "seconds" },
        };

    /// <summary>Whether the event type is allowlisted.</summary>
    public static bool IsKnownType(string eventType)
    {
        return AllowedProperties.ContainsKey(eventType);
    }

    /// <summary>Whether a property is allowlisted for the given event type.</summary>
    public static bool IsAllowedProperty(string eventType, string propertyName)
    {
        return AllowedProperties.TryGetValue(eventType, out var allowed) && allowed.Contains(propertyName);
    }
}
