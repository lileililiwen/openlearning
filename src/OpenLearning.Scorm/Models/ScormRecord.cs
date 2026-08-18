using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Scorm.Models;

public class ScormRecord
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public EnrollmentEntity? Enrollment { get; set; }

    public int ScormPackageId { get; set; }

    public ScormPackage? ScormPackage { get; set; }

    public string LessonLocation { get; set; } = string.Empty;

    public string SuspendData { get; set; } = string.Empty;

    /// <summary>SCORM 1.2: passed, completed, failed, incomplete, not attempted, browsed.</summary>
    public string LessonStatus { get; set; } = string.Empty;

    public string ScoreRaw { get; set; } = string.Empty;

    public string SessionTime { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
