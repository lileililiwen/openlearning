namespace OpenLearning.Notifications.Models;

/// <summary>
/// Per-user, per-type channel toggles for SMS and web push. A missing row
/// means the channels default to enabled; rows are created when a user opts
/// out. In-app delivery is always on (no toggle).
/// </summary>
public class NotificationPreference
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public bool SmsEnabled { get; set; } = true;

    public bool PushEnabled { get; set; } = true;
}
