namespace OpenLearning.Ecommerce.Models;

/// <summary>One account-balance credit or debit. The balance is the running sum.</summary>
public class BalanceLedger
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Positive = credit, negative = debit.</summary>
    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One loyalty-points credit or debit. The balance is the running sum.</summary>
public class PointsLedger
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Positive = credit, negative = debit.</summary>
    public int Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
