using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Notifications;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<NotificationService>();
        services.AddScoped<AnnouncementService>();
        services.AddSingleton<IEmailSender, NoopEmailSender>();
        return services;
    }
}
