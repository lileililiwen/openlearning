using OpenLearning.Auth.Models;

namespace OpenLearning.Invoicing.Models;

public enum InvoiceRequestStatus
{
    Requested = 0,
    Rejected = 1,
    Issued = 2,
}

public enum InvoiceType
{
    Normal = 0,
    RedLetter = 1,
}

/// <summary>A student's invoice request queued for finance review.</summary>
public class InvoiceRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string StudentUserId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? TaxId { get; set; }

    public InvoiceRequestStatus Status { get; set; } = InvoiceRequestStatus.Requested;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public string? Reason { get; set; }

    public int? InvoiceId { get; set; }
}

/// <summary>An issued invoice with a unique sequential number.</summary>
public class Invoice
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public InvoiceType Type { get; set; } = InvoiceType.Normal;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public string IssuedBy { get; set; } = string.Empty;

    public DateTime? VoidedAt { get; set; }

    public string? VoidReason { get; set; }

    /// <summary>For red letters: the original invoice this corrects.</summary>
    public int? OriginalInvoiceId { get; set; }
}
