using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Settlement.Models;

/// <summary>
/// One instructor settlement entry. Positive credits accrue on paid orders;
/// negative entries record refund reversals. The available balance is the
/// running sum minus reserved withdrawals.
/// </summary>
public class SettlementLedger
{
    public int Id { get; set; }

    public string InstructorId { get; set; } = string.Empty;

    public int? CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Positive = credit, negative = refund reversal.</summary>
    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
