using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Moderation.Services;

namespace OpenLearning.Moderation;

public static class ModerationModuleExtensions
{
    public static IServiceCollection AddModerationModule(this IServiceCollection services)
    {
        services.AddScoped<ContentReviewService>();
        return services;
    }
}
