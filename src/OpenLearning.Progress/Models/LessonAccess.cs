using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Progress.Models;

/// <summary>
/// Records the last time a Student opened a lesson within an enrollment so the
/// dashboards can offer an exact "continue learning" resume point.
/// </summary>
public class LessonAccess
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }
    public EnrollmentEntity? Enrollment { get; set; }

    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last saved playback position in seconds for video lessons.</summary>
    public int PlaybackPositionSeconds { get; set; }
}
