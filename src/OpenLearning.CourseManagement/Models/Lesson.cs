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

    /// <summary>
    /// Imported text reference to external lesson content (Excel outline import
    /// only). Informational — the player never embeds this; instructors attach
    /// managed media via the lesson edit page.
    /// </summary>
    public string? ContentUrlRef { get; set; }

    /// <summary>When true, non-enrolled visitors of a published course can view this lesson.</summary>
    public bool IsPreview { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>
    /// Bumped whenever the lesson content changes (edit) or its parent course is
    /// (re)published. Cached clients carry the revision they cached with each
    /// sync event so the server can detect a stale-revision event and surface
    /// it as a conflict rather than silently accepting progress against
    /// out-of-date content.
    /// </summary>
    public int ContentRevision { get; set; } = 1;
}
