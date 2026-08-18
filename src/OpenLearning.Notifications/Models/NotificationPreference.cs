namespace OpenLearning.Notifications.Models;

/// <summary>
/// Per-user, per-type channel toggles for in-app, email, SMS, and web push.
/// A missing row means all channels default to enabled; rows are created when
/// a user opts out.
/// </summary>
public class NotificationPreference
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public bool InAppEnabled { get; set; } = true;

    public bool EmailEnabled { get; set; } = true;

    public bool SmsEnabled { get; set; } = true;

    public bool PushEnabled { get; set; } = true;
}
