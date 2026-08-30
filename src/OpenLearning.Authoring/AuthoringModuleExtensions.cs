using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Authoring.Services;

namespace OpenLearning.Authoring;

public static class AuthoringModuleExtensions
{
    public static IServiceCollection AddAuthoringModule(this IServiceCollection services)
    {
        services.AddScoped<RevisionLifecycleService>();
        return services;
    }
}
