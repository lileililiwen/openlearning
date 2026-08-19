namespace OpenLearning.StudentIO.Models;

/// <summary>Row action applied when a row's Action column is blank.</summary>
public enum StudentRowAction
{
    Create = 0,
    CreateAndEnroll = 1,
    EnrollExisting = 2,
}

/// <summary>Summary of what the import mostly did, for the jobs list.</summary>
public enum StudentImportMode
{
    Mixed = 0,
    Create = 1,
    Enroll = 2,
}

public enum StudentImportJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
}

/// <summary>
/// Metadata and counters for one asynchronous student import. The job
/// lifecycle lives on <see cref="AsyncIOJob"/>; this row carries the importer,
/// the selected default mode, and mirrors the outcome for the jobs page.
/// </summary>
public class StudentImportJob
{
    public int Id { get; set; }

    /// <summary>The Admin/Finance/TA user who submitted the import.</summary>
    public string UserId { get; set; } = string.Empty;

    public StudentImportMode Mode { get; set; } = StudentImportMode.Mixed;

    /// <summary>Default action applied to rows with a blank Action column.</summary>
    public StudentRowAction DefaultAction { get; set; } = StudentRowAction.Create;

    public string FileKey { get; set; } = string.Empty;

    public int AsyncIOJobId { get; set; }

    public StudentImportJobStatus Status { get; set; } = StudentImportJobStatus.Pending;

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public string? ErrorFileKey { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One row-level validation error for a student import job.</summary>
public class StudentImportRowError
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int RowIndex { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
