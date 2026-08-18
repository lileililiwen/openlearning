using OpenLearning.Notifications.Models;

namespace OpenLearning.SystemConfig.Models;

/// <summary>
/// Admin-editable notification copy for a notification type. One template per
/// type; <see cref="IsActive"/> gates whether it is applied.
/// </summary>
public class NotificationTemplate
{
    public int Id { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
