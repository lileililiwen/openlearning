using System;
using System.Collections.Generic;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Services;

/// <summary>Thrown when the actor is not authorized to act on an integration or delivery.</summary>
public sealed class UnauthorizedIntegrationOperationException : Exception
{
    public UnauthorizedIntegrationOperationException()
    {
    }

    public UnauthorizedIntegrationOperationException(string message)
        : base(message)
    {
    }

    public UnauthorizedIntegrationOperationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>One field-level validation failure for an integration declaration.</summary>
public sealed class MappingValidationError
{
    public string Field { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

/// <summary>Request to declare a new external integration.</summary>
public sealed class DeclareIntegrationRequest
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public int ContractVersion { get; init; } = 1;

    public string CapabilitiesJson { get; init; } = "[]";

    public string? EndpointReference { get; init; }

    /// <summary>Reference to a stored secret; never the secret value.</summary>
    public string? SecretReference { get; init; }
}

/// <summary>Request to deliver an event to a declared integration.</summary>
public sealed class DeliverRequest
{
    public string EventType { get; init; } = string.Empty;

    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>Raw payload; the service redacts it before any persistence or audit.</summary>
    public string Payload { get; init; } = string.Empty;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>Outcome of declaring or enabling/disabling an integration.</summary>
public sealed class IntegrationMutationResult
{
    public bool Succeeded { get; init; }

    public List<MappingValidationError> Errors { get; init; } = new();

    public IntegrationRegistration? Registration { get; init; }
}

/// <summary>Outcome of a delivery or replay operation.</summary>
public sealed class IntegrationDeliveryResult
{
    public IntegrationDeliveryStatus Status { get; init; }

    public IntegrationErrorCategory ErrorCategory { get; init; } = IntegrationErrorCategory.None;

    public string Diagnostic { get; init; } = string.Empty;

    public int? DeliveryId { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>True when the result was served from a prior attempt (idempotency).</summary>
    public bool FromCache { get; init; }
}
