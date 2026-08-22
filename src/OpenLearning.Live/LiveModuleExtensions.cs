using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Live.Services;

namespace OpenLearning.Live;

public static class LiveModuleExtensions
{
    public static IServiceCollection AddLiveModule(this IServiceCollection services)
    {
        services.AddScoped<LiveService>();
        services.AddScoped<LiveBookingService>();
        return services;
    }
}
