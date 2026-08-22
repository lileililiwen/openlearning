namespace OpenLearning.Payments.Models;

public enum PaymentState { Pending, Succeeded, Failed, Cancelled }
public enum RefundState { Pending, Succeeded, Failed }
public enum ReconciliationState { Open, Resolved }

public sealed class PaymentIntent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "CNY";
    public PaymentState State { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SucceededAt { get; set; }
}

public sealed class PaymentAttempt
{
    public long Id { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string ProviderReference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PaymentRefund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public string ProviderRefundId { get; set; } = string.Empty;
    public RefundState State { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}

public sealed class ProviderEvent
{
    public long Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool SignatureValid { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PaymentOutbox
{
    public long Id { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string Kind { get; set; } = "FulfillOrder";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public sealed class PaymentReconciliationIssue
{
    public long Id { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool Retryable { get; set; }
    public ReconciliationState State { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
