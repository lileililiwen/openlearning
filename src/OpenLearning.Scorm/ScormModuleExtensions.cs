using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Scorm.Services;

namespace OpenLearning.Scorm;

public static class ScormModuleExtensions
{
    public static IServiceCollection AddScormModule(this IServiceCollection services)
    {
        services.AddScoped<ScormService>();
        services.AddScoped<ScormRuntimeService>();
        return services;
    }
}
