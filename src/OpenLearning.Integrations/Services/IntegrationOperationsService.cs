using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Services;

/// <summary>
/// Contract + delivery runtime for external integrations. Integrations declare a type,
/// contract version, capabilities, tenant scope, and enabled state before invocation.
/// Deliveries are idempotent (idempotency key), bounded (timeout + retry), and fall back
/// to a dead-letter state on exhaustion. All attempts are audited without secrets, and
/// replay preserves the original event identity. The module depends only on the base
/// <see cref="DbContext"/>, so protocol modules remain free of transport concerns.
/// </summary>
public sealed class IntegrationOperationsService
{
    /// <summary>Current supported contract version for each known integration type.</summary>
    public const int CurrentContractVersion = 1;

    private static readonly Dictionary<string, int> _supportedContractVersions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Webhook"] = 1,
        ["Lti"] = 1,
        ["Scorm"] = 1,
    };

    private static readonly Regex _tokenPattern = new("[A-Za-z0-9+/_]{16,}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _secretValuePattern = new(
        "\"(?<k>[^\"]*(secret|token|password|api_?key|authorization|signingkey)[^\"]*)\"\\s*:\\s*\"[^\"]*\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private readonly DbContext _db;
    private readonly IEnumerable<IIntegrationAdapter>? _adapters;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public IntegrationOperationsService(
        DbContext db,
        IEnumerable<IIntegrationAdapter>? adapters = null,
        int maxAttempts = 3,
        TimeSpan retryDelay = default)
    {
        _db = db;
        _adapters = adapters;
        _maxAttempts = maxAttempts > 0 ? maxAttempts : 3;
        _retryDelay = retryDelay;
    }

    /// <summary>Declares a new integration for a tenant (owner/admin only).</summary>
    public async Task<IntegrationMutationResult> DeclareIntegrationAsync(
        string tenantId, string actorId, DeclareIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<MappingValidationError>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new MappingValidationError { Field = nameof(request.Name), Code = "Required", Message = "Integration name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            errors.Add(new MappingValidationError { Field = nameof(request.Type), Code = "Required", Message = "Integration type is required." });
        }
        else if (!_supportedContractVersions.ContainsKey(request.Type))
        {
            errors.Add(new MappingValidationError
            {
                Field = nameof(request.Type),
                Code = "UnsupportedType",
                Message = $"Integration type must be one of: {string.Join(", ", _supportedContractVersions.Keys)}.",
            });
        }

        if (request.ContractVersion <= 0)
        {
            errors.Add(new MappingValidationError
            {
                Field = nameof(request.ContractVersion),
                Code = "Range",
                Message = "Contract version must be greater than 0.",
            });
        }

        if (errors.Count > 0)
        {
            return new IntegrationMutationResult { Succeeded = false, Errors = errors };
        }

        var registration = new IntegrationRegistration
        {
            Name = request.Name,
            Type = request.Type,
            ContractVersion = request.ContractVersion,
            CapabilitiesJson = string.IsNullOrWhiteSpace(request.CapabilitiesJson) ? "[]" : request.CapabilitiesJson,
            TenantId = tenantId,
            IsEnabled = true,
            EndpointReference = request.EndpointReference,
            SecretReference = request.SecretReference,
        };

        _db.Set<IntegrationRegistration>().Add(registration);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(tenantId, registration.Id, null, "Declare", "Succeeded", null, null, actorId, cancellationToken);
        return new IntegrationMutationResult { Succeeded = true, Registration = registration };
    }

    /// <summary>Enables or disables an integration (tenant-scoped).</summary>
    public async Task<IntegrationMutationResult> SetEnabledAsync(
        string tenantId, string actorId, int registrationId, bool enabled, CancellationToken cancellationToken = default)
    {
        var registration = await _db.Set<IntegrationRegistration>().FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);
        if (registration is null || registration.TenantId != tenantId)
        {
            throw new UnauthorizedIntegrationOperationException($"Integration {registrationId} is not accessible for tenant {tenantId}.");
        }

        registration.IsEnabled = enabled;
        registration.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var action = enabled ? "Enable" : "Disable";
        await RecordAuditAsync(tenantId, registration.Id, null, action, "Succeeded", null, null, actorId, cancellationToken);
        return new IntegrationMutationResult { Succeeded = true, Registration = registration };
    }

    /// <summary>Returns the integrations declared for a tenant.</summary>
    public async Task<List<IntegrationRegistration>> GetIntegrationsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Set<IntegrationRegistration>().AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Delivers an event to a declared integration. Fails closed for unknown contract
    /// versions or disabled integrations (no external request is made). Duplicate
    /// idempotency keys return the prior disposition without re-invoking the adapter.
    /// </summary>
    public async Task<IntegrationDeliveryResult> DeliverAsync(
        string tenantId, int registrationId, DeliverRequest request, CancellationToken cancellationToken = default)
    {
        var registration = await _db.Set<IntegrationRegistration>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);
        if (registration is null || registration.TenantId != tenantId)
        {
            throw new UnauthorizedIntegrationOperationException($"Integration {registrationId} is not accessible for tenant {tenantId}.");
        }

        if (!registration.IsEnabled)
        {
            await RecordAuditAsync(tenantId, registration.Id, null, "Dispatch", "Skipped", "Integration is disabled.", null, null, cancellationToken);
            return new IntegrationDeliveryResult
            {
                Status = IntegrationDeliveryStatus.Failed,
                ErrorCategory = IntegrationErrorCategory.Disabled,
                Diagnostic = "Integration is disabled.",
                IdempotencyKey = request.IdempotencyKey,
            };
        }

        if (!_supportedContractVersions.TryGetValue(registration.Type, out var supported) || registration.ContractVersion > supported)
        {
            var diagnostic = $"Contract version {registration.ContractVersion} for type '{registration.Type}' is not supported (max {supported}).";
            await RecordAuditAsync(tenantId, registration.Id, null, "Dispatch", "Failed", diagnostic, null, null, cancellationToken);
            return new IntegrationDeliveryResult
            {
                Status = IntegrationDeliveryStatus.Failed,
                ErrorCategory = IntegrationErrorCategory.UnknownVersion,
                Diagnostic = diagnostic,
                IdempotencyKey = request.IdempotencyKey,
            };
        }

        var existing = await _db.Set<IntegrationDelivery>().AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdempotencyKey == request.IdempotencyKey && d.TenantId == tenantId, cancellationToken);
        if (existing is not null)
        {
            await RecordAuditAsync(
                tenantId, registration.Id, existing.Id, "Dispatch", "Duplicate",
                $"Idempotency key {request.IdempotencyKey} already processed with status {existing.Status}; no external call made.",
                null, null, cancellationToken);
            return new IntegrationDeliveryResult
            {
                Status = existing.Status,
                IdempotencyKey = request.IdempotencyKey,
                FromCache = true,
            };
        }

        var delivery = new IntegrationDelivery
        {
            RegistrationId = registration.Id,
            IdempotencyKey = request.IdempotencyKey,
            EventType = request.EventType,
            PayloadReference = Redact(request.Payload),
            Status = IntegrationDeliveryStatus.Pending,
            TenantId = tenantId,
            Attempts = 0,
        };
        _db.Set<IntegrationDelivery>().Add(delivery);
        await _db.SaveChangesAsync(cancellationToken);

        return await DispatchAsync(registration, delivery, request, cancellationToken);
    }

    /// <summary>
    /// Replays a dead-lettered delivery. A new attempt is created preserving the original
    /// event identity (idempotency key + event type) and a replay audit record is written.
    /// Tenant scoping is enforced; a non-owning tenant receives no delivery.
    /// </summary>
    public async Task<IntegrationDeliveryResult> ReplayDeadLetterAsync(
        string tenantId, string actorId, int deliveryId, CancellationToken cancellationToken = default)
    {
        var original = await _db.Set<IntegrationDelivery>().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);
        if (original is null || original.TenantId != tenantId)
        {
            throw new UnauthorizedIntegrationOperationException($"Delivery {deliveryId} is not accessible for tenant {tenantId}.");
        }

        if (original.Status != IntegrationDeliveryStatus.DeadLettered)
        {
            return new IntegrationDeliveryResult
            {
                Status = original.Status,
                DeliveryId = deliveryId,
                IdempotencyKey = original.IdempotencyKey,
                Diagnostic = "Only dead-lettered deliveries can be replayed.",
            };
        }

        var registration = await _db.Set<IntegrationRegistration>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == original.RegistrationId, cancellationToken);
        if (registration is null)
        {
            throw new UnauthorizedIntegrationOperationException($"Registration {original.RegistrationId} not found.");
        }

        var replay = new IntegrationDelivery
        {
            RegistrationId = original.RegistrationId,
            IdempotencyKey = original.IdempotencyKey,
            EventType = original.EventType,
            PayloadReference = original.PayloadReference,
            Status = IntegrationDeliveryStatus.Pending,
            TenantId = tenantId,
            Attempts = 0,
        };
        _db.Set<IntegrationDelivery>().Add(replay);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(
            tenantId, original.RegistrationId, original.Id, "Replay", "Retried",
            $"Replaying dead-letter as delivery {replay.Id}.", original.PayloadReference, actorId, cancellationToken);

        var request = new DeliverRequest
        {
            EventType = original.EventType,
            IdempotencyKey = original.IdempotencyKey,
            Payload = original.PayloadReference,
            Timeout = TimeSpan.FromSeconds(10),
        };
        return await DispatchAsync(registration, replay, request, cancellationToken);
    }

    /// <summary>Returns recent deliveries and audit events for operator diagnostics (tenant-scoped).</summary>
    public async Task<List<IntegrationDelivery>> GetDeliveriesAsync(
        string tenantId, int? registrationId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<IntegrationDelivery>().AsNoTracking().Where(d => d.TenantId == tenantId);
        if (registrationId is not null)
        {
            query = query.Where(d => d.RegistrationId == registrationId.Value);
        }

        return await query.OrderByDescending(d => d.Id).Take(100).ToListAsync(cancellationToken);
    }

    /// <summary>Returns recent audit records for operator diagnostics (tenant-scoped).</summary>
    public async Task<List<IntegrationAuditRecord>> GetAuditAsync(
        string tenantId, int? registrationId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<IntegrationAuditRecord>().AsNoTracking().Where(a => a.TenantId == tenantId);
        if (registrationId is not null)
        {
            query = query.Where(a => a.RegistrationId == registrationId.Value);
        }

        return await query.OrderByDescending(a => a.Id).Take(200).ToListAsync(cancellationToken);
    }

    private async Task<IntegrationDeliveryResult> DispatchAsync(
        IntegrationRegistration registration, IntegrationDelivery delivery, DeliverRequest request, CancellationToken cancellationToken)
    {
        var adapter = (_adapters ?? Array.Empty<IIntegrationAdapter>()).FirstOrDefault(a => a.SupportsType(registration.Type));
        if (adapter is null)
        {
            return await DeadLetterAsync(registration, delivery, IntegrationErrorCategory.Permanent,
                "No adapter registered for this integration type.", request, cancellationToken);
        }

        var validate = await adapter.ValidateAsync(registration, cancellationToken);
        if (!validate.Succeeded)
        {
            var category = validate.Category == IntegrationErrorCategory.None ? IntegrationErrorCategory.Permanent : validate.Category;
            return await DeadLetterAsync(registration, delivery, category, validate.Diagnostic, request, cancellationToken);
        }

        var adapterRequest = new IntegrationDeliveryRequest
        {
            IdempotencyKey = request.IdempotencyKey,
            EventType = request.EventType,
            Payload = request.Payload,
            ContractVersion = registration.ContractVersion,
            Capabilities = ParseCapabilities(registration.CapabilitiesJson),
            Endpoint = ParseEndpoint(registration.EndpointReference),
            Timeout = request.Timeout,
        };

        var attemptsRemaining = _maxAttempts;
        while (attemptsRemaining > 0)
        {
            attemptsRemaining--;
            delivery.Attempts++;

            IntegrationResult outcome;
            try
            {
                outcome = await adapter.ExecuteAsync(registration, adapterRequest, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                outcome = IntegrationResult.Failure(IntegrationErrorCategory.Timeout, "Adapter was canceled.");
            }
            catch (Exception ex)
            {
                outcome = IntegrationResult.Failure(IntegrationErrorCategory.Transient, ex.Message);
            }

            if (outcome.Succeeded)
            {
                delivery.Status = IntegrationDeliveryStatus.Delivered;
                delivery.CompletedAt = DateTime.UtcNow;
                delivery.LastErrorCategory = IntegrationErrorCategory.None;
                delivery.LastError = null;
                await _db.SaveChangesAsync(cancellationToken);
                await RecordAuditAsync(delivery.TenantId, registration.Id, delivery.Id, "Dispatch", "Succeeded",
                    null, Redact(request.Payload), null, cancellationToken);
                return new IntegrationDeliveryResult
                {
                    Status = delivery.Status,
                    DeliveryId = delivery.Id,
                    IdempotencyKey = request.IdempotencyKey,
                };
            }

            var category = outcome.Category == IntegrationErrorCategory.None ? IntegrationErrorCategory.Transient : outcome.Category;
            delivery.LastErrorCategory = category;
            delivery.LastError = outcome.Diagnostic;

            var retryable = category is IntegrationErrorCategory.Transient or IntegrationErrorCategory.Timeout;
            if (!retryable || attemptsRemaining == 0)
            {
                return await DeadLetterAsync(registration, delivery, category, outcome.Diagnostic, request, cancellationToken);
            }

            delivery.Status = IntegrationDeliveryStatus.Failed;
            delivery.NextAttemptAt = DateTime.UtcNow.Add(_retryDelay);
            await _db.SaveChangesAsync(cancellationToken);
            await RecordAuditAsync(delivery.TenantId, registration.Id, delivery.Id, "Retry", "Failed",
                outcome.Diagnostic, Redact(request.Payload), null, cancellationToken);

            if (_retryDelay > TimeSpan.Zero)
            {
                await Task.Delay(_retryDelay, cancellationToken);
            }
        }

        return await DeadLetterAsync(registration, delivery, IntegrationErrorCategory.Transient,
            "Retry budget exhausted.", request, cancellationToken);
    }

    private async Task<IntegrationDeliveryResult> DeadLetterAsync(
        IntegrationRegistration registration, IntegrationDelivery delivery, IntegrationErrorCategory category,
        string diagnostic, DeliverRequest request, CancellationToken cancellationToken)
    {
        delivery.LastErrorCategory = category;
        delivery.LastError = diagnostic;
        delivery.Status = IntegrationDeliveryStatus.DeadLettered;
        delivery.NextAttemptAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(delivery.TenantId, registration.Id, delivery.Id, "DeadLetter", "Failed",
            diagnostic, Redact(request.Payload), null, cancellationToken);
        return new IntegrationDeliveryResult
        {
            Status = delivery.Status,
            ErrorCategory = category,
            Diagnostic = diagnostic,
            DeliveryId = delivery.Id,
            IdempotencyKey = request.IdempotencyKey,
        };
    }

    private async Task RecordAuditAsync(
        string tenantId, int? registrationId, int? deliveryId, string action, string disposition,
        string? detail, string? redactedPayload, string? actorId, CancellationToken cancellationToken)
    {
        _db.Set<IntegrationAuditRecord>().Add(new IntegrationAuditRecord
        {
            TenantId = tenantId,
            RegistrationId = registrationId,
            DeliveryId = deliveryId,
            Action = action,
            Disposition = disposition,
            Detail = detail,
            RedactedPayload = redactedPayload,
            ActorId = actorId,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static List<string> ParseCapabilities(string json)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static Uri? ParseEndpoint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    /// <summary>
    /// Masks secret-bearing material so audit/payload references never store raw secrets.
    /// Long token-like runs and JSON values of secret-ish keys are replaced.
    /// </summary>
    private static string Redact(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return payload;
        }

        var masked = _tokenPattern.Replace(payload, "***REDACTED***");
        masked = _secretValuePattern.Replace(masked, "\"${k}\":\"***REDACTED***\"");
        return masked;
    }
}
