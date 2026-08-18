namespace OpenLearning.Notifications.Email;

/// <summary>
/// Optional email channel for notifications. The default implementation is a
/// no-op; an SMTP implementation is registered when Email:Enabled is set.
/// Sending is best-effort and must never block in-app delivery.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string body);
}

/// <summary>Default no-op sender used when no email provider is configured.</summary>
public sealed class NoopEmailSender : IEmailSender
{
    public Task SendAsync(string toAddress, string subject, string body)
    {
        return Task.CompletedTask;
    }
}
