using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Logging.Services;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "用户管理")]
public class UsersModel : PageModel
{
    private readonly UserManagementService _users;
    private readonly LogService _logs;

    public UsersModel(UserManagementService users, LogService logs)
    {
        _users = users;
        _logs = logs;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public List<UserListItem> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _users.SearchUsersAsync(Search);
    }

    public async Task<IActionResult> OnPostToggleInstructorAsync(string userId, bool add)
    {
        var (ok, error) = await _users.SetRoleAsync(userId, Roles.Instructor, add);
        if (ok)
        {
            await RecordLog("ToggleInstructor", "User", userId, add ? "Added instructor role." : "Removed instructor role.");
        }

        return Flash(ok, error);
    }

    public async Task<IActionResult> OnPostSetSuspendedAsync(string userId, bool suspended)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == userId)
        {
            return Flash(false, "You cannot suspend your own account.");
        }

        var (ok, error) = await _users.SetSuspendedAsync(userId, suspended);
        if (ok)
        {
            await RecordLog(suspended ? "SuspendUser" : "UnsuspendUser", "User", userId, null);
        }

        return Flash(ok, error);
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

        return RedirectToPage(new { Search });
    }
}
