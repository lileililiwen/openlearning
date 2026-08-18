using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Notifications.Channels;
using OpenLearning.Notifications.Configuration;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Notifications;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<NotificationService>();
        services.AddScoped<AnnouncementService>();
        services.AddScoped<PushSubscriptionService>();
        services.AddSingleton<IEmailSender, NoopEmailSender>();
        services.AddSingleton<ISmsSender, NoopSmsSender>();
        services.AddSingleton<IWebPushSender, NoopWebPushSender>();
        services.AddScoped<INotificationTemplateRenderer, NullNotificationTemplateRenderer>();
        services.AddOptions<ChannelOptions>().BindConfiguration(ChannelOptions.SectionName);
        return services;
    }
}
