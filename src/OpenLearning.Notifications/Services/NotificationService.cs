using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenLearning.Auth.Models;
using OpenLearning.Notifications.Channels;
using OpenLearning.Notifications.Configuration;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Services;

public class NotificationService
{
    private readonly DbContext _db;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly IWebPushSender _push;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly ChannelOptions _channels;

    public NotificationService(
        DbContext db,
        IEmailSender email,
        ISmsSender sms,
        IWebPushSender push,
        INotificationTemplateRenderer renderer,
        IOptions<ChannelOptions> channels)
    {
        _db = db;
        _email = email;
        _sms = sms;
        _push = push;
        _renderer = renderer;
        _channels = channels.Value;
    }

    public async Task CreateAsync(
        string userId, NotificationType type, string title, string body, string? link = null,
        IReadOnlyDictionary<string, string>? values = null)
    {
        var (finalTitle, finalBody) = await RenderAsync(type, title, body, values);
        _db.Set<Notification>().Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = finalTitle,
            Body = finalBody,
            Link = link,
        });
        await _db.SaveChangesAsync();

        var (emailAddress, phoneNumber) = await GetContactAsync(userId);
        var (smsAllowed, pushAllowed) = await GetChannelPreferencesAsync(userId, type);

        // Fire-and-forget delivery on optional channels; failures never block in-app delivery.
        try
        {
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                await _email.SendAsync(emailAddress, $"[OpenLearning] {finalTitle}", $"{finalBody}\n\n{link ?? string.Empty}");
            }
        }
        catch
        {
            // Email is best-effort and optional.
        }

        if (_channels.SmsEnabled && smsAllowed && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            try
            {
                await _sms.SendAsync(phoneNumber, $"{finalTitle}: {finalBody}");
            }
            catch
            {
                // SMS is best-effort and optional.
            }
        }

        if (_channels.PushEnabled && pushAllowed)
        {
            try
            {
                await _push.SendAsync(userId, finalTitle, finalBody, link);
            }
            catch
            {
                // Push is best-effort and optional.
            }
        }
    }

    /// <summary>Creates one notification per user id (used for course-wide events).</summary>
    public async Task CreateForManyAsync(
        IEnumerable<string> userIds, NotificationType type, string title, string body, string? link = null,
        IReadOnlyDictionary<string, string>? values = null)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var (finalTitle, finalBody) = await RenderAsync(type, title, body, values);
        foreach (var userId in ids)
        {
            _db.Set<Notification>().Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = finalTitle,
                Body = finalBody,
                Link = link,
            });
        }
        await _db.SaveChangesAsync();

        var users = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.PhoneNumber })
            .ToListAsync();
        var preferences = await _db.Set<NotificationPreference>().AsNoTracking()
            .Where(p => ids.Contains(p.UserId) && p.Type == type)
            .ToDictionaryAsync(p => p.UserId);

        try
        {
            foreach (var userId in ids)
            {
                var emailAddress = users.FirstOrDefault(e => e.Id == userId)?.Email;
                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    await _email.SendAsync(emailAddress, $"[OpenLearning] {finalTitle}", $"{finalBody}\n\n{link ?? string.Empty}");
                }
            }
        }
        catch
        {
            // Best-effort.
        }

        foreach (var userId in ids)
        {
            var phoneNumber = users.FirstOrDefault(u => u.Id == userId)?.PhoneNumber;
            var smsAllowed = preferences.GetValueOrDefault(userId)?.SmsEnabled ?? true;
            if (_channels.SmsEnabled && smsAllowed && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                try
                {
                    await _sms.SendAsync(phoneNumber, $"{finalTitle}: {finalBody}");
                }
                catch
                {
                    // Best-effort.
                }
            }

            var pushAllowed = preferences.GetValueOrDefault(userId)?.PushEnabled ?? true;
            if (_channels.PushEnabled && pushAllowed)
            {
                try
                {
                    await _push.SendAsync(userId, finalTitle, finalBody, link);
                }
                catch
                {
                    // Best-effort.
                }
            }
        }
    }

    private async Task<(string? Email, string? Phone)> GetContactAsync(string userId)
    {
        var contact = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.PhoneNumber })
            .FirstOrDefaultAsync();
        return (contact?.Email, contact?.PhoneNumber);
    }

    private async Task<(bool Sms, bool Push)> GetChannelPreferencesAsync(string userId, NotificationType type)
    {
        var preference = await _db.Set<NotificationPreference>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type);
        return (preference?.SmsEnabled ?? true, preference?.PushEnabled ?? true);
    }

    private async Task<(string Title, string Body)> RenderAsync(
        NotificationType type, string title, string body, IReadOnlyDictionary<string, string>? values)
    {
        var rendered = await _renderer.RenderAsync(type, title, body, values);
        return rendered ?? (title, body);
    }

    public Task<List<Notification>> GetRecentAsync(string userId, int count = 30)
    {
        return _db.Set<Notification>().AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(string userId)
    {
        return _db.Set<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    /// <summary>Marks a single notification read; only its owner may do so.</summary>
    public async Task<bool> MarkReadAsync(int notificationId, string userId)
    {
        var notification = await _db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(string userId)
    {
        var unread = await _db.Set<Notification>()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }
        await _db.SaveChangesAsync();
        return unread.Count;
    }
}
