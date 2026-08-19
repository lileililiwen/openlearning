using OpenLearning.Navigation.Services;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Navigation;

/// <summary>Unread-notification count for the sidebar Notifications badge.</summary>
public sealed class NotificationsNavCounter : INavCounterProvider
{
    private readonly NotificationService _notifications;

    public NotificationsNavCounter(NotificationService notifications)
    {
        _notifications = notifications;
    }

    public string Key => "notifications.unread";

    public Task<int> GetCountAsync(string userId)
    {
        return _notifications.GetUnreadCountAsync(userId);
    }
}
