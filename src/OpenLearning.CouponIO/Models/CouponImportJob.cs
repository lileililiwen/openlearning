namespace OpenLearning.CouponIO.Models;

public enum CouponImportJobStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
}

/// <summary>
/// Metadata and counters for one bulk coupon import. The actual job lifecycle
/// (status, notifications, error-file link) lives on
/// <see cref="OpenLearning.AsyncIO.Models.AsyncIOJob"/>; this row carries the
/// import parameters and mirrors the outcome for the admin import-jobs page.
/// </summary>
public class CouponImportJob
{
    public int Id { get; set; }

    /// <summary>Admin who submitted the import.</summary>
    public string UserId { get; set; } = string.Empty;

    public string FileKey { get; set; } = string.Empty;

    public int AsyncIOJobId { get; set; }

    public CouponImportJobStatus Status { get; set; } = CouponImportJobStatus.Pending;

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public string? ErrorFileKey { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One row-level validation error for a coupon import job.</summary>
public class CouponImportRowError
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int RowIndex { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
