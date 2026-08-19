namespace OpenLearning.Ecommerce.Models;

/// <summary>A Student's request for an invoice on a paid order (printing deferred).</summary>
public class InvoiceRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
