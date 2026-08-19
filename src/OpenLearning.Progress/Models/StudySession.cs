using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Progress.Models;

/// <summary>
/// One lesson study session. The client starts a session when the lesson page
/// loads, sends a heartbeat every ~60s while visible, and ends it when the tab
/// is hidden or closed. Duration accumulates on each heartbeat; heartbeats more
/// than two intervals apart are treated as idle and do not count.
/// </summary>
public class StudySession
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    /// <summary>The enrollment this session belongs to, when known.</summary>
    public int? EnrollmentId { get; set; }
    public EnrollmentEntity? Enrollment { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last heartbeat timestamp; used to exclude idle gaps.</summary>
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public int DurationSeconds { get; set; }
}
