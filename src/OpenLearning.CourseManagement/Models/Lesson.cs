namespace OpenLearning.CourseManagement.Models;

public class Lesson
{
    public int Id { get; set; }

    public int ModuleId { get; set; }

    public Module? Module { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>Stored video URL; when set the lesson renders through the video player.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Optional poster image URL for the video player.</summary>
    public string? VideoPosterUrl { get; set; }

    /// <summary>Optional WebVTT subtitle track URL.</summary>
    public string? SubtitleUrl { get; set; }

    /// <summary>When true, non-enrolled visitors of a published course can view this lesson.</summary>
    public bool IsPreview { get; set; }

    public int OrderIndex { get; set; }
}
