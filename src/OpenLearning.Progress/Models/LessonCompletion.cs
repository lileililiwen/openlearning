using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Progress.Models;

public class LessonCompletion
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public EnrollmentEntity? Enrollment { get; set; }

    public int LessonId { get; set; }

    public Lesson? Lesson { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
