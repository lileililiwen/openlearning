namespace OpenLearning.Integrations.Models;

/// <summary>Lifecycle state of a single delivery attempt.</summary>
public enum IntegrationDeliveryStatus
{
    Pending = 0,
    Dispatched = 1,
    Delivered = 2,
    Failed = 3,
    DeadLettered = 4,
}

/// <summary>Protocol-neutral classification of a delivery outcome.</summary>
public enum IntegrationErrorCategory
{
    None = 0,
    UnknownVersion = 1,
    Transient = 2,
    Timeout = 3,
    Permanent = 4,
    Unauthorized = 5,
    Disabled = 6,
    Duplicate = 7,
}

/// <summary>
/// Declares an external integration and the contract it supports. Integrations must
/// be declared (type, contract version, capabilities, tenant scope, enabled state)
/// before they can be invoked. The <see cref="SecretReference"/> stores only a
/// reference to a secret, never the secret value itself.
/// </summary>
public sealed class IntegrationRegistration
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Discriminator for the protocol: "Webhook", "Lti", or "Scorm".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Contract version this registration was authored against.</summary>
    public int ContractVersion { get; set; } = 1;

    /// <summary>JSON array of capability strings declared by the integration.</summary>
    public string CapabilitiesJson { get; set; } = "[]";

    /// <summary>Tenant that owns the integration; used for isolation and scoping.</summary>
    public string TenantId { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    /// <summary>Reference to the external endpoint (URL) used for delivery.</summary>
    public string? EndpointReference { get; set; }

    /// <summary>Reference to a stored secret (e.g. a key id); never the secret value.</summary>
    public string? SecretReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One delivery attempt grouped by an idempotency key. Retries update the same row;
/// exhaustion moves the row to <see cref="IntegrationDeliveryStatus.DeadLettered"/>.
/// Sensitive payloads are never persisted — only a redacted reference is kept.
/// </summary>
public sealed class IntegrationDelivery
{
    public int Id { get; set; }

    public int RegistrationId { get; set; }

    /// <summary>Caller-supplied idempotency key; duplicate keys reuse the prior result.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    /// <summary>Redacted payload reference; never contains the raw secret-bearing payload.</summary>
    public string PayloadReference { get; set; } = string.Empty;

    public IntegrationDeliveryStatus Status { get; set; } = IntegrationDeliveryStatus.Pending;

    public int Attempts { get; set; }

    public IntegrationErrorCategory? LastErrorCategory { get; set; }

    public string? LastError { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Immutable record of an integration action (declare, dispatch, retry, dead-letter,
/// replay, enable/disable) without secrets or sensitive payloads.
/// </summary>
public sealed class IntegrationAuditRecord
{
    public int Id { get; set; }

    public int? DeliveryId { get; set; }

    public int? RegistrationId { get; set; }

    /// <summary>Action name: Declare, Dispatch, Retry, DeadLetter, Replay, Enable, Disable.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Outcome of the action: Succeeded, Failed, Skipped, Retried.</summary>
    public string Disposition { get; set; } = string.Empty;

    public string? Detail { get; set; }

    /// <summary>Redacted payload or reference; secrets are masked before storage.</summary>
    public string? RedactedPayload { get; set; }

    public string? ActorId { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
