namespace OpenLearning.StudyTools.Models;

public enum NoteContextType
{
    Course = 0,
    Lesson = 1,
    Resource = 2,
}

/// <summary>
/// A learner's private note anchored to a course, lesson, or resource context.
/// Multiple notes per context are allowed; tags are stored as a comma-separated string.
/// </summary>
public class LearnerNote
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Plain Markdown body, sanitized before rendering.</summary>
    public string Body { get; set; } = string.Empty;

    public NoteContextType ContextType { get; set; }

    /// <summary>Id of the course, lesson, or resource this note references.</summary>
    public int ContextId { get; set; }

    /// <summary>Optional media timestamp in seconds (for video context).</summary>
    public int? MediaOffsetSeconds { get; set; }

    /// <summary>Comma-separated tags for filtering.</summary>
    public string? Tags { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
