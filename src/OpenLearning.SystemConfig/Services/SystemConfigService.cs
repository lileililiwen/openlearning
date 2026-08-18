using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Notifications.Models;
using OpenLearning.SystemConfig.Models;

namespace OpenLearning.SystemConfig.Services;

/// <summary>
/// Reads and writes admin-managed key-value settings and notification
/// templates. Typed getters parse with code fallbacks so bad values never
/// break callers.
/// </summary>
public class SystemConfigService
{
    private readonly DbContext _db;

    public SystemConfigService(DbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _db.Set<Setting>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task<string> GetStringAsync(string key, string fallback)
    {
        var value = await GetAsync(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    public async Task<int> GetIntAsync(string key, int fallback)
    {
        var value = await GetAsync(key);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    public async Task<bool> GetBoolAsync(string key, bool fallback)
    {
        var value = await GetAsync(key);
        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _db.Set<Setting>().FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            _db.Set<Setting>().Add(new Setting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await _db.SaveChangesAsync();
    }

    public async Task SetManyAsync(IReadOnlyDictionary<string, string> values)
    {
        foreach (var (key, value) in values)
        {
            await SetAsync(key, value);
        }
    }

    public Task<List<NotificationTemplate>> GetTemplatesAsync()
    {
        return _db.Set<NotificationTemplate>().AsNoTracking()
            .OrderBy(t => t.Type)
            .ToListAsync();
    }

    public async Task UpdateTemplateAsync(int id, string title, string body, bool isActive)
    {
        var template = await _db.Set<NotificationTemplate>().FindAsync(id);
        if (template is null)
        {
            return;
        }

        template.Title = title;
        template.Body = body;
        template.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Renders notification copy from the active template for the type, or
    /// returns null when no active template exists. Unknown tokens render as-is.
    /// </summary>
    public async Task<(string Title, string Body)?> RenderAsync(
        NotificationType type,
        string fallbackTitle,
        string fallbackBody,
        IReadOnlyDictionary<string, string>? values)
    {
        var template = await _db.Set<NotificationTemplate>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.Type == type && t.IsActive);
        if (template is null)
        {
            return null;
        }

        return (Render(template.Title, values), Render(template.Body, values));
    }

    private static string Render(string input, IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return input;
        }

        var result = input;
        foreach (var (key, value) in values)
        {
            result = result.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return result;
    }
}
