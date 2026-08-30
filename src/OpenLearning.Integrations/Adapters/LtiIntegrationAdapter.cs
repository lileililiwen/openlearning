using System;
using System.Threading;
using System.Threading.Tasks;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Adapters;

/// <summary>
/// Adapts an LTI integration entry point to the shared contract. The protocol details
/// remain owned by the LTI module; this adapter performs the outbound delivery
/// (e.g. an LTI outcome/service notification) against the registered endpoint.
/// </summary>
public sealed class LtiIntegrationAdapter : HttpIntegrationAdapterBase
{
    public override bool SupportsType(string integrationType)
    {
        return string.Equals(integrationType, "Lti", StringComparison.OrdinalIgnoreCase);
    }

    public override Task<IntegrationResult> ValidateAsync(
        IntegrationRegistration registration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registration.EndpointReference)
            || !Uri.TryCreate(registration.EndpointReference, UriKind.Absolute, out _))
        {
            return Task.FromResult(IntegrationResult.Failure(
                IntegrationErrorCategory.Permanent, "LTI integration requires a valid absolute endpoint URL."));
        }

        return Task.FromResult(IntegrationResult.Success());
    }
}
