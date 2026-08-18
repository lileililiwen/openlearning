using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Scorm.Models;

public class ScormPackage
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public Lesson? Lesson { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ScormVersion { get; set; } = "1.2";

    /// <summary>Relative path of the launchable SCO within the package folder.</summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>Relative path under wwwroot, e.g. "scorm/1".</summary>
    public string PackagePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
