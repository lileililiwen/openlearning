namespace OpenLearning.Settlement.Models;

/// <summary>Frozen per-instructor earnings for a closed settlement period.</summary>
public class SettlementStatement
{
    public int Id { get; set; }

    public string InstructorId { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal NetAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
