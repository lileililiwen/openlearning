using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.SystemConfig.Services;

/// <summary>
/// Renders notification copy through the SystemConfig templates. Registered
/// after <see cref="NullNotificationTemplateRenderer"/> so it wins resolution.
/// </summary>
public sealed class SystemConfigTemplateRenderer : INotificationTemplateRenderer
{
    private readonly SystemConfigService _config;

    public SystemConfigTemplateRenderer(SystemConfigService config)
    {
        _config = config;
    }

    public Task<(string Title, string Body)?> RenderAsync(
        NotificationType type,
        string fallbackTitle,
        string fallbackBody,
        IReadOnlyDictionary<string, string>? values)
    {
        return _config.RenderAsync(type, fallbackTitle, fallbackBody, values);
    }
}
