namespace OpenLearning.GradeExport.Models;

/// <summary>Which data set a grade export covers.</summary>
public enum GradeExportKind
{
    Submissions = 0,
    QuizAttempts = 1,
    ExamAttempts = 2,
    CourseRoster = 3,
}

/// <summary>Lifecycle of one grade export job.</summary>
public enum GradeExportJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}

/// <summary>
/// Audit + retention record for a grade export. The heavy lifting (files,
/// notifications, result keys) lives on the paired AsyncIOJob; this row keeps
/// the export-specific parameters and the retention hook for the cleanup job.
/// </summary>
public class GradeExportJob
{
    public int Id { get; set; }

    /// <summary>Exporter id (instructor, admin, or TA).</summary>
    public string UserId { get; set; } = string.Empty;

    public GradeExportKind Kind { get; set; }

    /// <summary>Serialized <c>GradeExportFilters</c> used to run (or re-run) the export.</summary>
    public string? FiltersJson { get; set; }

    /// <summary>Stored result file key; cleared by the retention cleanup.</summary>
    public string? FileKey { get; set; }

    public GradeExportJobStatus Status { get; set; } = GradeExportJobStatus.Pending;

    public int RowCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAt { get; set; }

    /// <summary>Paired async-io job that executes this export.</summary>
    public int? AsyncIOJobId { get; set; }
}
