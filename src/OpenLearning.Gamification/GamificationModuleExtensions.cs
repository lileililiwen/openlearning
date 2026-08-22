using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Gamification.Services;

namespace OpenLearning.Gamification;

public static class GamificationModuleExtensions
{
    public static IServiceCollection AddGamificationModule(this IServiceCollection services)
    {
        services.AddScoped<GamificationService>();
        return services;
    }
}
