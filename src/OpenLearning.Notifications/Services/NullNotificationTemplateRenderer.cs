using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Services;

/// <summary>Default no-op renderer; real implementations register after this one.</summary>
public sealed class NullNotificationTemplateRenderer : INotificationTemplateRenderer
{
    public Task<(string Title, string Body)?> RenderAsync(
        NotificationType type,
        string fallbackTitle,
        string fallbackBody,
        IReadOnlyDictionary<string, string>? values)
    {
        return Task.FromResult<(string Title, string Body)?>(null);
    }
}
