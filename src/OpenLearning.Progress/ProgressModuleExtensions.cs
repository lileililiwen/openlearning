using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Progress.Services;

namespace OpenLearning.Progress;

public static class ProgressModuleExtensions
{
    public static IServiceCollection AddProgressModule(this IServiceCollection services)
    {
        services.AddScoped<ProgressService>();
        return services;
    }
}
