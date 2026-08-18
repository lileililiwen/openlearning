using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Logging.Services;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class UserDetailModel : PageModel
{
    private readonly UserManagementService _users;
    private readonly LogService _logs;

    public UserDetailModel(UserManagementService users, LogService logs)
    {
        _users = users;
        _logs = logs;
    }

    public UserDetailItem? Detail { get; set; }

    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync(string id, string? search)
    {
        Detail = await _users.GetUserDetailAsync(id);
        if (Detail is null)
        {
            return NotFound();
        }

        Search = search;
        return Page();
    }

    public async Task<IActionResult> OnPostToggleRoleAsync(string userId, string role, bool add)
    {
        var (ok, error) = await _users.SetRoleAsync(userId, role, add);
        if (ok)
        {
            await RecordLog("ToggleRole", "User", userId, add ? $"Added {role}." : $"Removed {role}.");
        }

        return Flash(ok, error, userId);
    }

    public async Task<IActionResult> OnPostSetSuspendedAsync(string userId, bool suspended)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == userId)
        {
            return Flash(false, "You cannot suspend your own account.", userId);
        }

        var (ok, error) = await _users.SetSuspendedAsync(userId, suspended);
        if (ok)
        {
            await RecordLog(suspended ? "SuspendUser" : "UnsuspendUser", "User", userId, null);
        }

        return Flash(ok, error, userId);
    }

    private Task RecordLog(string action, string targetType, string targetId, string? details)
    {
        return _logs.RecordAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name ?? string.Empty,
            action,
            targetType,
            targetId,
            details,
            HttpContext.Connection.RemoteIpAddress?.ToString());
    }

    private RedirectToPageResult Flash(bool ok, string? error, string userId)
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

        return RedirectToPage(new { id = userId, search = Search });
    }
}
