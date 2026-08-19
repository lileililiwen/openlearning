namespace OpenLearning.Settlement.Models;

public enum WithdrawalStatus
{
    Pending = 0,
    Paid = 1,
    Rejected = 2,
}

/// <summary>An Instructor's payout request; money movement is external.</summary>
public class WithdrawalRequest
{
    public int Id { get; set; }

    public string InstructorId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }
}
