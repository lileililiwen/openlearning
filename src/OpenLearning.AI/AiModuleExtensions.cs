using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AI.Services;

namespace OpenLearning.AI;

public static class AiModuleExtensions
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddSingleton<IAiProvider, SandboxAiProvider>();
        services.AddScoped<AiLearningService>();
        return services;
    }
}
