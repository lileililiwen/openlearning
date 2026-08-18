using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Logging.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class IdentitiesModel : PageModel
{
    private readonly IdentityService _identities;
    private readonly NotificationService _notifications;
    private readonly LogService _logs;

    public IdentitiesModel(
        IdentityService identities,
        NotificationService notifications,
        LogService logs)
    {
        _identities = identities;
        _notifications = notifications;
        _logs = logs;
    }

    public List<ApplicationUser> Pending { get; set; } = new();

    public List<ApplicationUser> Reviewed { get; set; } = new();

    public async Task OnGetAsync()
    {
        Pending = await _identities.GetPendingAsync();
        Reviewed = await _identities.GetReviewedAsync(20);
    }

    public async Task<IActionResult> OnPostReviewAsync(string id, string action, string? note)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _identities.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var (ok, error) = action == "approve"
            ? await _identities.ApproveAsync(id, note)
            : await _identities.RejectAsync(id, note);
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
            return RedirectToPage();
        }

        var approved = action == "approve";
        await _notifications.CreateAsync(
            id,
            NotificationType.Application,
            approved ? "Identity verified" : "Identity verification rejected",
            approved
                ? "Your identity has been verified. Instructors can now publish courses."
                : $"Your identity verification was not approved. Reason: {note ?? "Not specified."}",
            null,
            new Dictionary<string, string>
            {
                ["Status"] = approved ? "approved" : "rejected",
                ["Reason"] = note ?? "Not specified.",
            });

        await _logs.RecordAsync(
            reviewerId,
            User.Identity?.Name ?? string.Empty,
            approved ? "ApproveIdentity" : "RejectIdentity",
            "User",
            id,
            user.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Message"] = approved
            ? $"Approved {user.Email}."
            : $"Rejected {user.Email}.";
        return RedirectToPage();
    }
}
