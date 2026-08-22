using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenLearning.Auth.Models;
using OpenLearning.Notifications.Channels;
using OpenLearning.Notifications.Configuration;
using OpenLearning.Notifications.Email;
using OpenLearning.Notifications.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

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
        IReadOnlyDictionary<string, string>? values = null, int? classGroupId = null)
    {
        var (finalTitle, finalBody) = await RenderAsync(type, title, body, values);
        var (inAppAllowed, emailAllowed, smsAllowed, pushAllowed) =
            await GetChannelPreferencesAsync(userId, type);

        if (inAppAllowed)
        {
            _db.Set<Notification>().Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = finalTitle,
                Body = finalBody,
                Link = link,
                ClassGroupId = classGroupId,
            });
            await _db.SaveChangesAsync();
        }

        var (emailAddress, phoneNumber) = await GetContactAsync(userId);

        // Fire-and-forget delivery on optional channels; failures never block in-app delivery.
        try
        {
            if (emailAllowed && !string.IsNullOrWhiteSpace(emailAddress))
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

    /// <summary>Maps a template event key (e.g. <c>assignment.graded</c>) to its type and fallback copy.</summary>
    public static class EventKeys
    {
        public const string AssignmentGraded = "assignment.graded";
        public const string ExamStartingSoon = "exam.starting-soon";
        public const string AssignmentDueSoon = "assignment.due-soon";
        public const string AssignmentDueMissed = "assignment.due-missed";
        public const string ClassStartingSoon = "class.starting-soon";
        public const string EnrollmentExpiringSoon = "enrollment.expiring-soon";
        public const string EnrollmentExpired = "enrollment.expired";
        public const string OrderExpired = "order.expired";
        public const string RefundTimeoutRejected = "refund.timeout-rejected";
        public const string InvoiceIssued = "invoice.issued";
        public const string InvoiceRejected = "invoice.rejected";
        public const string InvoiceVoided = "invoice.voided";
        public const string InvoiceRedLetterIssued = "invoice.red-letter-issued";
        public const string ImportCompleted = "import.completed";
        public const string ImportFailed = "import.failed";
        public const string ExportReady = "export.ready";
        public const string ExportProgress = "export.progress";
        public const string AccountWelcome = "account.welcome";
        public const string EnrollmentGrantedBulk = "enrollment.granted-bulk";
        public const string IntegrityDisposition = "integrity.disposition";
    }

    private static readonly Dictionary<string, (NotificationType Type, string Title, string Body)> _events =
        new()
        {
            [EventKeys.AssignmentGraded] = (NotificationType.AssignmentGraded, "Assignment graded", "Your submission has been graded."),
            [EventKeys.ExamStartingSoon] = (NotificationType.ExamStartingSoon, "Exam starting soon", "An exam you are enrolled in starts within 30 minutes."),
            [EventKeys.AssignmentDueSoon] = (NotificationType.AssignmentDueSoon, "Assignment due soon", "An assignment is due within 24 hours."),
            [EventKeys.AssignmentDueMissed] = (NotificationType.AssignmentDueMissed, "Assignment missed", "You did not submit an assignment by its due date."),
            [EventKeys.ClassStartingSoon] = (NotificationType.ClassStartingSoon, "Class starting soon", "A class you are enrolled in starts within 30 minutes."),
            [EventKeys.EnrollmentExpiringSoon] = (NotificationType.EnrollmentExpiringSoon, "Course access expiring soon", "Your course access expires within 7 days."),
            [EventKeys.EnrollmentExpired] = (NotificationType.EnrollmentExpired, "Course access expired", "Your course access has ended."),
            [EventKeys.OrderExpired] = (NotificationType.OrderExpired, "Order expired", "Your unpaid order was cancelled."),
            [EventKeys.RefundTimeoutRejected] = (NotificationType.RefundTimeoutRejected, "Refund request closed", "Your refund request was not approved within the review window."),
            [EventKeys.InvoiceIssued] = (NotificationType.InvoiceIssued, "Invoice issued", "Your invoice is ready."),
            [EventKeys.InvoiceRejected] = (NotificationType.InvoiceRejected, "Invoice request rejected", "Your invoice request was rejected."),
            [EventKeys.InvoiceVoided] = (NotificationType.InvoiceVoided, "Invoice voided", "An invoice was voided."),
            [EventKeys.InvoiceRedLetterIssued] = (NotificationType.InvoiceRedLetterIssued, "Red-letter invoice issued", "A red-letter invoice has been issued."),
            [EventKeys.ImportCompleted] = (NotificationType.ImportCompleted, "Import completed", "Your import job has finished."),
            [EventKeys.ImportFailed] = (NotificationType.ImportFailed, "Import failed", "Your import job could not be completed."),
            [EventKeys.ExportReady] = (NotificationType.ExportReady, "Export ready", "Your export file is ready to download."),
            [EventKeys.ExportProgress] = (NotificationType.ExportProgress, "Export in progress", "Your export is still running."),
            [EventKeys.AccountWelcome] = (NotificationType.AccountWelcome, "Welcome", "Welcome to OpenLearning!"),
            [EventKeys.EnrollmentGrantedBulk] = (NotificationType.EnrollmentGrantedBulk, "Course access granted", "You have been enrolled in courses."),
            [EventKeys.IntegrityDisposition] = (NotificationType.IntegrityDisposition, "Exam integrity outcome", "A decision was recorded on your exam integrity incident."),
        };

    /// <summary>
    /// Sends a template-driven event to one user. The title/body come from the
    /// seeded template (when active) with the caller's placeholders; otherwise
    /// the registry fallback copy is used.
    /// </summary>
    public Task SendAsync(string key, string userId, IReadOnlyDictionary<string, string>? values = null, string? link = null)
    {
        if (!_events.TryGetValue(key, out var spec))
        {
            return Task.CompletedTask;
        }

        return CreateAsync(userId, spec.Type, spec.Title, spec.Body, link, values);
    }

    /// <summary>Creates one notification per user id for a template-driven event.</summary>
    public async Task SendForManyAsync(
        string key, IEnumerable<string> userIds, IReadOnlyDictionary<string, string>? values = null, string? link = null)
    {
        if (!_events.TryGetValue(key, out var spec))
        {
            return;
        }

        await CreateForManyAsync(userIds, spec.Type, spec.Title, spec.Body, link, values);
    }

    /// <summary>
    /// Sends a template-driven, class-scoped event to every active student
    /// enrolled in the class, tagging each notification with the class id.
    /// </summary>
    public async Task SendClassScopedAsync(
        string key, int classGroupId, IReadOnlyDictionary<string, string>? values = null, string? link = null)
    {
        if (!_events.TryGetValue(key, out var spec))
        {
            return;
        }

        var studentIds = await _db.Set<EnrollmentEntity>()
            .Where(e => e.ClassGroupId == classGroupId && e.RevokedAt == null)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync();
        foreach (var studentId in studentIds)
        {
            await CreateAsync(studentId, spec.Type, spec.Title, spec.Body, link, values, classGroupId);
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
        var users = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.PhoneNumber })
            .ToListAsync();
        var preferences = await _db.Set<NotificationPreference>().AsNoTracking()
            .Where(p => ids.Contains(p.UserId) && p.Type == type)
            .ToDictionaryAsync(p => p.UserId);

        var inAppRecipients = ids.Where(id =>
            preferences.GetValueOrDefault(id)?.InAppEnabled ?? true).ToList();
        foreach (var userId in inAppRecipients)
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
            foreach (var userId in ids)
            {
                var emailAddress = users.FirstOrDefault(e => e.Id == userId)?.Email;
                var emailAllowed = preferences.GetValueOrDefault(userId)?.EmailEnabled ?? true;
                if (emailAllowed && !string.IsNullOrWhiteSpace(emailAddress))
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

    private async Task<(bool InApp, bool Email, bool Sms, bool Push)> GetChannelPreferencesAsync(string userId, NotificationType type)
    {
        var preference = await _db.Set<NotificationPreference>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type);
        return (
            preference?.InAppEnabled ?? true,
            preference?.EmailEnabled ?? true,
            preference?.SmsEnabled ?? true,
            preference?.PushEnabled ?? true);
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

    /// <summary>Sends a class-scoped announcement to every student enrolled in the class.</summary>
    public async Task SendClassAnnouncementAsync(int classGroupId, string title, string body, string senderId)
    {
        var studentIds = await _db.Set<EnrollmentEntity>()
            .Where(e => e.ClassGroupId == classGroupId)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync();
        foreach (var studentId in studentIds)
        {
            await CreateAsync(
                studentId,
                NotificationType.Announcement,
                title,
                body,
                null,
                new Dictionary<string, string> { ["SenderId"] = senderId },
                classGroupId);
        }
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
