using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Adapters;

/// <summary>
/// Shared HTTP-based adapter behavior for outbound integrations. Concrete adapters
/// only supply their type discriminator and validation rules; all network delivery,
/// timeout enforcement, and failure classification live here so protocol modules stay
/// free of transport concerns. The single shared <see cref="HttpClient"/> is safe for
/// concurrent use and avoids socket exhaustion.
/// </summary>
public abstract class HttpIntegrationAdapterBase : IIntegrationAdapter
{
    private static readonly HttpClient _httpClient = new();

    public abstract bool SupportsType(string integrationType);

    public abstract Task<IntegrationResult> ValidateAsync(IntegrationRegistration registration, CancellationToken cancellationToken = default);

    public async Task<IntegrationResult> ExecuteAsync(
        IntegrationRegistration registration, IntegrationDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Endpoint is null)
        {
            return IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "No endpoint configured for delivery.");
        }

        try
        {
            // Enforce the per-delivery timeout independent of the caller's token.
            using var timeout = new CancellationTokenSource(request.Timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            var content = new StringContent(request.Payload, System.Text.Encoding.UTF8, "application/json");
            AddAuthHeader(content, registration);

            var response = await _httpClient.PostAsync(request.Endpoint, content, linked.Token);

            if (response.IsSuccessStatusCode)
            {
                return IntegrationResult.Success();
            }

            // 4xx are permanent failures; 5xx are transient and eligible for retry.
            var category = ((int)response.StatusCode >= 500)
                ? IntegrationErrorCategory.Transient
                : IntegrationErrorCategory.Permanent;
            return IntegrationResult.Failure(category, $"Provider returned {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return IntegrationResult.Failure(IntegrationErrorCategory.Timeout, "Provider exceeded the configured timeout.");
        }
        catch (Exception ex)
        {
            return IntegrationResult.Failure(IntegrationErrorCategory.Transient, ex.Message);
        }
    }

    public async Task<IntegrationResult> HealthAsync(
        IntegrationRegistration registration, CancellationToken cancellationToken = default)
    {
        if (registration.EndpointReference is null)
        {
            return IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "No endpoint configured.");
        }

        if (!Uri.TryCreate(registration.EndpointReference, UriKind.Absolute, out var endpoint))
        {
            return IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "Endpoint is not a valid absolute URI.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return IntegrationResult.Success();
        }
        catch (Exception ex)
        {
            return IntegrationResult.Failure(IntegrationErrorCategory.Transient, ex.Message);
        }
    }

    /// <summary>
    /// Hook for protocol-specific authentication. The base implementation adds nothing;
    /// subclasses may attach a header derived from <see cref="IntegrationRegistration.SecretReference"/>
    /// (never the secret value, which is not available here).
    /// </summary>
    protected virtual void AddAuthHeader(StringContent content, IntegrationRegistration registration)
    {
    }
}
