namespace OpenLearning.CourseOutlineIO.Models;

/// <summary>How an outline import treats the course's existing structure.</summary>
public enum OutlineImportMode
{
    /// <summary>Only new modules/lessons are created; existing outline is kept.</summary>
    Append = 0,

    /// <summary>Existing modules and lessons are wiped, then the file is re-imported.</summary>
    Replace = 1,
}

public enum OutlineImportJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
}

/// <summary>
/// Metadata and counters for one course-outline import. The actual job
/// lifecycle (status, notifications, error-file link) lives on
/// <see cref="OpenLearning.AsyncIO.Models.AsyncIOJob"/>; this row carries the
/// import parameters and mirrors the outcome for the import-jobs page.
/// </summary>
public class OutlineImportJob
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public OutlineImportMode Mode { get; set; } = OutlineImportMode.Append;

    public string FileKey { get; set; } = string.Empty;

    public int AsyncIOJobId { get; set; }

    public OutlineImportJobStatus Status { get; set; } = OutlineImportJobStatus.Pending;

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public string? ErrorFileKey { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One row-level validation error for an outline import job.</summary>
public class OutlineRowError
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int RowIndex { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
