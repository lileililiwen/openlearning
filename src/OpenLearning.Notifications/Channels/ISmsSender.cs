namespace OpenLearning.Notifications.Channels;

/// <summary>
/// Optional SMS channel. The default implementation is a no-op; an adapter
/// is registered when Messaging:Sms:Enabled is set. Sending is best-effort
/// and must never block in-app delivery.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message);
}

/// <summary>Default no-op SMS sender used when no SMS provider is configured.</summary>
public sealed class NoopSmsSender : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message)
    {
        return Task.CompletedTask;
    }
}
