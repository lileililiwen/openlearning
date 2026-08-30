using System;
using System.Threading;
using System.Threading.Tasks;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Adapters;

/// <summary>
/// Adapts a SCORM integration entry point to the shared contract. The SCORM module
/// remains responsible for package/runtime details; this adapter performs the
/// outbound delivery against the registered endpoint.
/// </summary>
public sealed class ScormIntegrationAdapter : HttpIntegrationAdapterBase
{
    public override bool SupportsType(string integrationType)
    {
        return string.Equals(integrationType, "Scorm", StringComparison.OrdinalIgnoreCase);
    }

    public override Task<IntegrationResult> ValidateAsync(
        IntegrationRegistration registration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registration.EndpointReference)
            || !Uri.TryCreate(registration.EndpointReference, UriKind.Absolute, out _))
        {
            return Task.FromResult(IntegrationResult.Failure(
                IntegrationErrorCategory.Permanent, "SCORM integration requires a valid absolute endpoint URL."));
        }

        return Task.FromResult(IntegrationResult.Success());
    }
}
