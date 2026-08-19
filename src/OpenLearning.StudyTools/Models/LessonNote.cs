using OpenLearning.CourseManagement.Models;

namespace OpenLearning.StudyTools.Models;

/// <summary>A Student's private notes on one lesson (one note per student/lesson).</summary>
public class LessonNote
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
