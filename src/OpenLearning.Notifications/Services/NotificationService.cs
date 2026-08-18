using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Services;

public class NotificationService
{
    private readonly DbContext _db;
    private readonly IEmailSender _email;
    private readonly INotificationTemplateRenderer _renderer;

    public NotificationService(DbContext db, IEmailSender email, INotificationTemplateRenderer renderer)
    {
        _db = db;
        _email = email;
        _renderer = renderer;
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

        // Fire-and-forget email delivery; failures never block in-app delivery.
        try
        {
            var emailAddress = await _db.Set<ApplicationUser>().AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                await _email.SendAsync(emailAddress, $"[OpenLearning] {finalTitle}", $"{finalBody}\n\n{link ?? string.Empty}");
            }
        }
        catch
        {
            // Email is best-effort and optional.
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

        try
        {
            var emails = await _db.Set<ApplicationUser>().AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();
            foreach (var userId in ids)
            {
                var emailAddress = emails.FirstOrDefault(e => e.Id == userId)?.Email;
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
