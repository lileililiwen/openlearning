using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Logging.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.UserManagement.Models;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class InstructorApplicationsModel : PageModel
{
    private readonly InstructorApplicationService _applications;
    private readonly NotificationService _notifications;
    private readonly LogService _logs;

    public InstructorApplicationsModel(
        InstructorApplicationService applications,
        NotificationService notifications,
        LogService logs)
    {
        _applications = applications;
        _notifications = notifications;
        _logs = logs;
    }

    public List<InstructorApplication> Pending { get; set; } = new();

    public List<InstructorApplication> Reviewed { get; set; } = new();

    public async Task OnGetAsync()
    {
        Pending = await _applications.GetPendingAsync();
        Reviewed = await _applications.GetReviewedAsync(20);
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var application = await _applications.GetByIdAsync(id);
        var (ok, error) = await _applications.ApproveAsync(id, reviewerId);
        if (ok && application is not null)
        {
            await _notifications.CreateAsync(
                application.UserId,
                NotificationType.Application,
                "Instructor application approved",
                "You can now create and publish courses. Welcome aboard!",
                "/Courses/Create");
            await _logs.RecordAsync(
                reviewerId,
                User.Identity?.Name ?? string.Empty,
                "ApproveInstructorApplication",
                "InstructorApplication",
                id.ToString(CultureInfo.InvariantCulture),
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        return Flash(ok, error);
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string? reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var application = await _applications.GetByIdAsync(id);
        var (ok, error) = await _applications.RejectAsync(id, reviewerId, reason);
        if (ok && application is not null)
        {
            await _notifications.CreateAsync(
                application.UserId,
                NotificationType.Application,
                "Instructor application not approved",
                $"Reason: {reason ?? "Not specified."}",
                null);
            await _logs.RecordAsync(
                reviewerId,
                User.Identity?.Name ?? string.Empty,
                "RejectInstructorApplication",
                "InstructorApplication",
                id.ToString(CultureInfo.InvariantCulture),
                reason,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        return Flash(ok, error);
    }

    private RedirectToPageResult Flash(bool ok, string? error)
    {
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
        }
        else
        {
            TempData["Message"] = "Saved.";
            TempData["MessageType"] = "success";
        }

        return RedirectToPage();
    }
}
