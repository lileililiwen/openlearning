using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Notifications.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.SystemConfig;

public static class SystemConfigModuleExtensions
{
    public static IServiceCollection AddSystemConfigModule(this IServiceCollection services)
    {
        services.AddScoped<SystemConfigService>();
        // Registered after AddNotificationsModule's no-op so this one wins.
        services.AddScoped<INotificationTemplateRenderer, SystemConfigTemplateRenderer>();
        return services;
    }
}
