using System;
using System.Threading;
using System.Threading.Tasks;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Adapters;

/// <summary>Generic outbound webhook adapter. Posts the event payload to the registered endpoint.</summary>
public sealed class WebhookIntegrationAdapter : HttpIntegrationAdapterBase
{
    public override bool SupportsType(string integrationType)
    {
        return string.Equals(integrationType, "Webhook", StringComparison.OrdinalIgnoreCase);
    }

    public override Task<IntegrationResult> ValidateAsync(
        IntegrationRegistration registration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registration.EndpointReference)
            || !Uri.TryCreate(registration.EndpointReference, UriKind.Absolute, out _))
        {
            return Task.FromResult(IntegrationResult.Failure(
                IntegrationErrorCategory.Permanent, "Webhook requires a valid absolute endpoint URL."));
        }

        return Task.FromResult(IntegrationResult.Success());
    }
}
