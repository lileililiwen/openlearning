namespace OpenLearning.AsyncIO.Models;

public enum AsyncIOJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
}

/// <summary>One persisted asynchronous import/export job.</summary>
public class AsyncIOJob
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    /// <summary>Consumer-specific parameters (e.g. export filters) serialized as JSON.</summary>
    public string? Payload { get; set; }

    public string FileKey { get; set; } = string.Empty;

    public string? ResultFileKey { get; set; }

    public AsyncIOJobStatus Status { get; set; } = AsyncIOJobStatus.Pending;

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public string? ErrorFileKey { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One row-level error for an import job, written to the error file.</summary>
public class AsyncIORowError
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int RowIndex { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
