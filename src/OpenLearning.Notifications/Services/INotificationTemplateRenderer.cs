using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Services;

/// <summary>
/// Renders notification copy from a template, or returns null when no active
/// template applies (the caller's text is then used as-is). Defined here so
/// the Notifications module stays independent of any templating module.
/// </summary>
public interface INotificationTemplateRenderer
{
    Task<(string Title, string Body)?> RenderAsync(
        NotificationType type,
        string fallbackTitle,
        string fallbackBody,
        IReadOnlyDictionary<string, string>? values);
}
