using OpenLearning.CourseManagement.Models;

namespace OpenLearning.StudyTools.Models;

/// <summary>
/// A file the course owner has configured as downloadable for a lesson.
/// Only rows with <see cref="IsAllowed"/> are offered to enrolled Students.
/// </summary>
public class LessonDownload
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    /// <summary>Stored file URL (from the storage module).</summary>
    public string FileUrl { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool IsAllowed { get; set; }
}
