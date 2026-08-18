using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly NotificationService _notifications;

    public IndexModel(NotificationService notifications)
    {
        _notifications = notifications;
    }

    public List<Notification> Items { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Items = await _notifications.GetRecentAsync(userId);
    }

    /// <summary>Marks a single notification read and follows its link.</summary>
    public async Task<IActionResult> OnPostOpenAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var notification = await _notifications.MarkReadAsync(id, userId);

        var link = notification
            ? (await GetNotificationLinkAsync(id))
            : null;
        return link is null ? RedirectToPage() : Redirect(link);
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _notifications.MarkAllReadAsync(userId);
        return RedirectToPage();
    }

    private async Task<string?> GetNotificationLinkAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var notification = (await _notifications.GetRecentAsync(userId))
            .FirstOrDefault(n => n.Id == id);
        return notification?.Link;
    }
}
