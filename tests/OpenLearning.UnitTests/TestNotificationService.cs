using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenLearning.Data;
using OpenLearning.Notifications.Channels;
using OpenLearning.Notifications.Configuration;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Services;

namespace OpenLearning.UnitTests;

/// <summary>Builds a NotificationService wired to no-op senders and the null template renderer.</summary>
public static class TestNotificationService
{
    public static NotificationService Create(ApplicationDbContext db)
    {
        return new NotificationService(
            db,
            new NoopEmailSender(),
            new NoopSmsSender(),
            new NoopPushSender(),
            new NullNotificationTemplateRenderer(),
            Options.Create(new ChannelOptions()));
    }

    private sealed class NoopEmailSender : IEmailSender
    {
        public Task SendAsync(string toAddress, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopSmsSender : ISmsSender
    {
        public Task SendAsync(string phoneNumber, string message)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopPushSender : IWebPushSender
    {
        public Task SendAsync(string userId, string title, string body, string? link)
        {
            return Task.CompletedTask;
        }
    }
}
