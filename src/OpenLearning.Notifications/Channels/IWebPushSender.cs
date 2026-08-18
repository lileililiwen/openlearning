namespace OpenLearning.Notifications.Channels;

/// <summary>
/// Optional web-push channel. The default implementation is a no-op; a
/// VAPID-backed sender is registered when Messaging:Push:Enabled is set.
/// Sending is best-effort and must never block in-app delivery.
/// </summary>
public interface IWebPushSender
{
    Task SendAsync(string userId, string title, string body, string? link);
}

/// <summary>Default no-op push sender used when web push is not configured.</summary>
public sealed class NoopWebPushSender : IWebPushSender
{
    public Task SendAsync(string userId, string title, string body, string? link)
    {
        return Task.CompletedTask;
    }
}
