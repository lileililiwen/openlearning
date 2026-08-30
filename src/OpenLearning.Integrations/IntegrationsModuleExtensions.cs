using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Integrations.Adapters;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Services;

namespace OpenLearning.Integrations;

public static class IntegrationsModuleExtensions
{
    /// <summary>
    /// Registers the integration contract runtime and the built-in protocol adapters
    /// (Webhook, LTI, SCORM). Additional protocol modules can register their own
    /// <see cref="IIntegrationAdapter"/> implementations without modifying this module.
    /// </summary>
    public static IServiceCollection AddIntegrationsModule(this IServiceCollection services)
    {
        services.AddScoped<IntegrationOperationsService>();
        services.AddScoped<IIntegrationAdapter, WebhookIntegrationAdapter>();
        services.AddScoped<IIntegrationAdapter, LtiIntegrationAdapter>();
        services.AddScoped<IIntegrationAdapter, ScormIntegrationAdapter>();
        return services;
    }
}
