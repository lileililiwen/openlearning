using Microsoft.Extensions.DependencyInjection;
using OpenLearning.ResourceCenter.Services;

namespace OpenLearning.ResourceCenter;

public static class ResourceCenterModuleExtensions
{
    public static IServiceCollection AddResourceCenterModule(this IServiceCollection services)
    {
        services.AddScoped<ResourceService>();
        return services;
    }
}
