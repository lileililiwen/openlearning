using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Web.Pages.Account;

[Authorize]
public class NotificationsModel : PageModel
{
    private readonly DbContext _db;

    public NotificationsModel(DbContext db)
    {
        _db = db;
    }

    public class PreferenceInput
    {
        public NotificationType Type { get; set; }

        public bool SmsEnabled { get; set; }

        public bool PushEnabled { get; set; }
    }

    public List<PreferenceInput> Preferences { get; set; } = new();

    [BindProperty]
    public List<PreferenceInput> Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        foreach (var item in Input)
        {
            var existing = await _db.Set<NotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == item.Type);
            if (existing is null)
            {
                _db.Set<NotificationPreference>().Add(new NotificationPreference
                {
                    UserId = userId,
                    Type = item.Type,
                    SmsEnabled = item.SmsEnabled,
                    PushEnabled = item.PushEnabled,
                });
            }
            else
            {
                existing.SmsEnabled = item.SmsEnabled;
                existing.PushEnabled = item.PushEnabled;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = "Notification preferences saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var stored = await _db.Set<NotificationPreference>().AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Type);

        Preferences = Enum.GetValues<NotificationType>()
            .Select(type => new PreferenceInput
            {
                Type = type,
                SmsEnabled = stored.TryGetValue(type, out var p) && p.SmsEnabled,
                PushEnabled = stored.TryGetValue(type, out p) && p.PushEnabled,
            })
            .ToList();
    }
}
