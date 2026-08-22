using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Lti.Services;

namespace OpenLearning.Lti;

public static class LtiModuleExtensions
{
    public static IServiceCollection AddLtiModule(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<LtiProtocolService>();
        services.AddScoped<LtiAdvantageService>();
        services.AddScoped<LtiAdminService>();
        return services;
    }
}
