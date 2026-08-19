using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Notifications.Models;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class SystemModel : PageModel
{
    private readonly SystemConfigService _config;

    public SystemModel(SystemConfigService config)
    {
        _config = config;
    }

    /// <summary>A whitelisted, editable setting. Only keys code actually reads are shown.</summary>
    public class SettingItem
    {
        public string Key { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public class TemplateInput
    {
        public int Id { get; set; }

        public NotificationType Type { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    private static readonly SettingItem[] _whitelist =
    {
        new() { Key = "Site.Name", Label = "Site name", Description = "Shown in the header, page titles, and footer." },
        new() { Key = "Catalog.PageSize", Label = "Catalog page size", Description = "Courses per page on the public catalog (1–50)." },
        new() { Key = "enrollment.expiry.graceDays", Label = "Enrollment expiry grace days", Description = "Days a learner keeps read-only access after a course's access period expires (0–365)." },
        new() { Key = "logging.retention.days", Label = "Log retention (days)", Description = "How many days operation/error logs are kept before the logs.archive job prunes them (1–3650)." },
        new() { Key = "invoice.nextNumber", Label = "Next invoice number", Description = "The next invoice number that will be allocated (default 100000)." },
        new() { Key = "invoice.prefix", Label = "Invoice number prefix", Description = "Optional prefix prepended to invoice numbers (default empty)." },
        new() { Key = "invoice.padding", Label = "Invoice number padding", Description = "Zero-pad the numeric part of invoice numbers to this width (1–20, default 6)." },
    };

    public List<SettingItem> Settings { get; set; } = new();

    [BindProperty]
    public List<TemplateInput> TemplatesInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadSettingsAsync();
        await LoadTemplatesAsync();
    }

    public async Task<IActionResult> OnPostSettingsAsync(Dictionary<string, string> settings)
    {
        var valid = new Dictionary<string, string>();
        foreach (var key in _whitelist.Select(item => item.Key))
        {
            var value = settings.GetValueOrDefault(key) ?? string.Empty;
            if (key == "Catalog.PageSize" && (!int.TryParse(value, out var pageSize) || pageSize < 1 || pageSize > 50))
            {
                return Flash("Catalog page size must be a whole number between 1 and 50.", "danger");
            }

            if (key == "enrollment.expiry.graceDays" &&
                (!int.TryParse(value, out var graceDays) || graceDays < 0 || graceDays > 365))
            {
                return Flash("Enrollment expiry grace days must be a whole number between 0 and 365.", "danger");
            }

            if (key == "logging.retention.days" &&
                (!int.TryParse(value, out var retentionDays) || retentionDays < 1 || retentionDays > 3650))
            {
                return Flash("Log retention days must be a whole number between 1 and 3650.", "danger");
            }

            valid[key] = value;
        }

        await _config.SetManyAsync(valid);
        return Flash("Settings saved.", "success");
    }

    public async Task<IActionResult> OnPostTemplatesAsync()
    {
        foreach (var input in TemplatesInput)
        {
            var title = input.Title.Trim();
            var body = input.Body.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200 ||
                string.IsNullOrWhiteSpace(body) || body.Length > 2000)
            {
                return Flash("Every template needs a title (≤200 chars) and body (≤2000 chars).", "danger");
            }

            await _config.UpdateTemplateAsync(input.Id, title, body, input.IsActive);
        }

        return Flash("Notification templates saved.", "success");
    }

    private async Task LoadSettingsAsync()
    {
        foreach (var item in _whitelist)
        {
            Settings.Add(new SettingItem
            {
                Key = item.Key,
                Label = item.Label,
                Description = item.Description,
                Value = await _config.GetAsync(item.Key) ?? string.Empty,
            });
        }
    }

    private async Task LoadTemplatesAsync()
    {
        var templates = await _config.GetTemplatesAsync();
        TemplatesInput = templates
            .Select(t => new TemplateInput
            {
                Id = t.Id,
                Type = t.Type,
                Title = t.Title,
                Body = t.Body,
                IsActive = t.IsActive,
            })
            .ToList();
    }

    private RedirectToPageResult Flash(string message, string type)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = type;
        return RedirectToPage();
    }
}
