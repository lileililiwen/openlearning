using Microsoft.Extensions.DependencyInjection;
using OpenLearning.LearningPaths.Services;

namespace OpenLearning.LearningPaths;

public static class LearningPathsModuleExtensions
{
    public static IServiceCollection AddLearningPathsModule(this IServiceCollection services)
    {
        services.AddScoped<LearningPathService>();
        return services;
    }
}
