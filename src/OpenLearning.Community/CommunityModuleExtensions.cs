using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Community.Services;

namespace OpenLearning.Community;

public static class CommunityModuleExtensions
{
    public static IServiceCollection AddCommunityModule(this IServiceCollection services)
    {
        services.AddScoped<CommunityService>();
        return services;
    }
}
