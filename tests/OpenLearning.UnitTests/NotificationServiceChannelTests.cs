using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenLearning.Auth.Models;
using OpenLearning.Data;
using OpenLearning.Notifications.Channels;
using OpenLearning.Notifications.Configuration;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using Xunit;

namespace OpenLearning.UnitTests.Notifications;

public sealed class NotificationServiceChannelTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendAsync(string toAddress, string subject, string body)
        {
            Sent.Add((toAddress, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSmsSender : ISmsSender
    {
        public List<(string Phone, string Message)> Sent { get; } = new();

        public Task SendAsync(string phoneNumber, string message)
        {
            Sent.Add((phoneNumber, message));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPushSender : IWebPushSender
    {
        public List<(string UserId, string Title, string Body, string? Link)> Sent { get; } = new();

        public Task SendAsync(string userId, string title, string body, string? link)
        {
            Sent.Add((userId, title, body, link));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPushSender : IWebPushSender
    {
        public Task SendAsync(string userId, string title, string body, string? link)
        {
            throw new InvalidOperationException("push down");
        }
    }

    private static ChannelOptions DefaultChannels(bool sms = true, bool push = true)
    {
        return new ChannelOptions
        {
            SmsEnabled = sms,
            PushEnabled = push,
        };
    }

    private static NotificationService CreateService(
        ApplicationDbContext db,
        IEmailSender email,
        ISmsSender sms,
        IWebPushSender push,
        ChannelOptions options)
    {
        return new NotificationService(
            db,
            email,
            sms,
            push,
            new NullNotificationTemplateRenderer(),
            Options.Create(options));
    }

    [Fact]
    public async Task CreateAsync_dispatches_sms_and_push_when_enabled()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
            Email = "u1@example.com",
            PhoneNumber = "+15551234567",
        });
        await db.SaveChangesAsync();

        var email = new RecordingEmailSender();
        var sms = new RecordingSmsSender();
        var push = new RecordingPushSender();
        var service = CreateService(db, email, sms, push, DefaultChannels());

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Single(sms.Sent);
        Assert.Equal("+15551234567", sms.Sent[0].Phone);
        Assert.Single(push.Sent);
        Assert.Equal("u1", push.Sent[0].UserId);
    }

    [Fact]
    public async Task CreateAsync_skips_disabled_channels()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
            Email = "u1@example.com",
            PhoneNumber = "+15551234567",
        });
        await db.SaveChangesAsync();

        var email = new RecordingEmailSender();
        var sms = new RecordingSmsSender();
        var push = new RecordingPushSender();
        var service = CreateService(db, email, sms, push, DefaultChannels(sms: false, push: false));

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Empty(sms.Sent);
        Assert.Empty(push.Sent);
        Assert.Single(db.Set<Notification>()); // in-app always delivered
    }

    [Fact]
    public async Task CreateAsync_skips_sms_without_phone()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
            Email = "u1@example.com",
        });
        await db.SaveChangesAsync();

        var sms = new RecordingSmsSender();
        var service = CreateService(db, new RecordingEmailSender(), sms, new RecordingPushSender(), DefaultChannels());

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Empty(sms.Sent);
    }

    [Fact]
    public async Task CreateAsync_honors_preferences_per_type()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
            PhoneNumber = "+15551234567",
        });
        db.Set<NotificationPreference>().Add(new NotificationPreference
        {
            UserId = "u1",
            Type = NotificationType.Course,
            SmsEnabled = false,
            PushEnabled = false,
        });
        await db.SaveChangesAsync();

        var sms = new RecordingSmsSender();
        var push = new RecordingPushSender();
        var service = CreateService(db, new RecordingEmailSender(), sms, push, DefaultChannels());

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Empty(sms.Sent);
        Assert.Empty(push.Sent);
        Assert.Single(db.Set<Notification>());
    }

    [Fact]
    public async Task CreateAsync_failure_on_push_does_not_block_in_app()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
        });
        await db.SaveChangesAsync();

        var sms = new RecordingSmsSender();
        var service = CreateService(db, new RecordingEmailSender(), sms, new ThrowingPushSender(), DefaultChannels());

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Single(db.Set<Notification>());
    }

    [Fact]
    public async Task CreateForManyAsync_dispatches_per_user_preferences()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().AddRange(
            new ApplicationUser { Id = "u1", UserName = "u1", PhoneNumber = "+15550000001" },
            new ApplicationUser { Id = "u2", UserName = "u2", PhoneNumber = "+15550000002" });
        db.Set<NotificationPreference>().Add(new NotificationPreference
        {
            UserId = "u2",
            Type = NotificationType.Lesson,
            SmsEnabled = false,
        });
        await db.SaveChangesAsync();

        var sms = new RecordingSmsSender();
        var push = new RecordingPushSender();
        var service = CreateService(db, new RecordingEmailSender(), sms, push, DefaultChannels());

        await service.CreateForManyAsync(_testUserIds, NotificationType.Lesson, "Title", "Body");

        Assert.Single(sms.Sent); // only u1
        Assert.Equal("+15550000001", sms.Sent[0].Phone);
        Assert.Equal(2, push.Sent.Count);
        Assert.Equal(2, await db.Set<Notification>().CountAsync());
    }

    [Fact]
    public async Task CreateAsync_skips_in_app_when_preference_disables_it()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
        });
        db.Set<NotificationPreference>().Add(new NotificationPreference
        {
            UserId = "u1",
            Type = NotificationType.Course,
            InAppEnabled = false,
        });
        await db.SaveChangesAsync();

        var email = new RecordingEmailSender();
        var service = CreateService(db, email, new RecordingSmsSender(), new RecordingPushSender(), DefaultChannels());

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Empty(await db.Set<Notification>().ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_skips_email_when_preference_disables_it()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
            Email = "u1@example.com",
        });
        db.Set<NotificationPreference>().Add(new NotificationPreference
        {
            UserId = "u1",
            Type = NotificationType.Course,
            EmailEnabled = false,
        });
        await db.SaveChangesAsync();

        var email = new RecordingEmailSender();
        var service = CreateService(db, email, new RecordingSmsSender(), new RecordingPushSender(), DefaultChannels());

        await service.CreateAsync("u1", NotificationType.Course, "Title", "Body");

        Assert.Empty(email.Sent);
        Assert.Single(db.Set<Notification>()); // in-app still delivered
    }

    [Fact]
    public async Task CreateForManyAsync_filters_in_app_recipients_by_preference()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().AddRange(
            new ApplicationUser { Id = "u1", UserName = "u1" },
            new ApplicationUser { Id = "u2", UserName = "u2" });
        db.Set<NotificationPreference>().Add(new NotificationPreference
        {
            UserId = "u2",
            Type = NotificationType.Lesson,
            InAppEnabled = false,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new RecordingEmailSender(), new RecordingSmsSender(), new RecordingPushSender(), DefaultChannels());

        await service.CreateForManyAsync(_testUserIds, NotificationType.Lesson, "Title", "Body");

        var notifications = await db.Set<Notification>().ToListAsync();
        Assert.Single(notifications);
        Assert.Equal("u1", notifications[0].UserId);
    }

    private static readonly string[] _testUserIds = { "u1", "u2" };
}
