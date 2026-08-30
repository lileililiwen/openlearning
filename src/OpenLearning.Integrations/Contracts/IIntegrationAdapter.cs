using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Contracts;

/// <summary>A protocol-neutral request handed to an adapter for execution.</summary>
public sealed class IntegrationDeliveryRequest
{
    public string IdempotencyKey { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    /// <summary>Redacted-or-raw payload; adapters must treat it as opaque and never log it.</summary>
    public string Payload { get; init; } = string.Empty;

    public int ContractVersion { get; init; } = 1;

    public IReadOnlyList<string> Capabilities { get; init; } = new List<string>();

    public Uri? Endpoint { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>Protocol-neutral result returned by an adapter.</summary>
public sealed class IntegrationResult
{
    public bool Succeeded { get; init; }

    public IntegrationErrorCategory Category { get; init; } = IntegrationErrorCategory.None;

    public string Diagnostic { get; init; } = string.Empty;

    public static IntegrationResult Success()
    {
        return new() { Succeeded = true };
    }

    public static IntegrationResult Failure(IntegrationErrorCategory category, string diagnostic)
    {
        return new() { Succeeded = false, Category = category, Diagnostic = diagnostic };
    }
}

/// <summary>
/// Common boundary every protocol adapter exposes. Protocol modules (LTI, SCORM,
/// webhooks) provide their own implementations; this contract keeps the delivery
/// runtime protocol-neutral. Adapters must fail closed and never bypass authorization.
/// </summary>
public interface IIntegrationAdapter
{
    /// <summary>Whether this adapter handles the given integration type discriminator.</summary>
    bool SupportsType(string integrationType);

    /// <summary>Validates that the registration can be invoked (endpoint, capabilities).</summary>
    Task<IntegrationResult> ValidateAsync(IntegrationRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>Performs the external delivery. Must honor <see cref="IntegrationDeliveryRequest.Timeout"/>.</summary>
    Task<IntegrationResult> ExecuteAsync(IntegrationRegistration registration, IntegrationDeliveryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reports adapter/provider health for operator diagnostics.</summary>
    Task<IntegrationResult> HealthAsync(IntegrationRegistration registration, CancellationToken cancellationToken = default);
}
