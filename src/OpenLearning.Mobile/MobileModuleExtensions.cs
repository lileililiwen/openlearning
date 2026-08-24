using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Mobile.Services;

namespace OpenLearning.Mobile;

public static class MobileModuleExtensions
{
    public static IServiceCollection AddMobileModule(this IServiceCollection services)
    {
        services.AddScoped<MobileSessionService>();
        services.AddScoped<OfflineManifestService>();
        services.AddScoped<MobileSyncService>();
        services.AddScoped<MobilePushService>();
        return services;
    }
}
