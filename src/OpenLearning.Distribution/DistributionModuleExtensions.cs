using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Distribution;

public static class DistributionModuleExtensions
{
    public static IServiceCollection AddDistributionModule(this IServiceCollection services)
    {
        services.AddScoped<DistributionService>();
        return services;
    }
}
