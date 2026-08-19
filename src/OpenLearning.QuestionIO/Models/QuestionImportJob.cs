namespace OpenLearning.QuestionIO.Models;

public enum QuestionImportMode
{
    Append = 0,
    UpdateOrAppend = 1,
}

public enum QuestionImportJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
}

/// <summary>
/// Metadata and counters for one asynchronous question import. The actual job
/// lifecycle (status, notifications, error-file link) lives on
/// <see cref="AsyncIOJob"/>; this row carries the import parameters and mirrors
/// the outcome for the quiz import-jobs page.
/// </summary>
public class QuestionImportJob
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int? QuizId { get; set; }

    public bool IsBank { get; set; }

    public QuestionImportMode Mode { get; set; } = QuestionImportMode.Append;

    public string FileKey { get; set; } = string.Empty;

    public int AsyncIOJobId { get; set; }

    public QuestionImportJobStatus Status { get; set; } = QuestionImportJobStatus.Pending;

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public string? ErrorFileKey { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One row-level validation error for an import job.</summary>
public class QuestionRowError
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int RowIndex { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
