using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class UsersModel : PageModel
{
    private readonly UserManagementService _users;

    public UsersModel(UserManagementService users)
    {
        _users = users;
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

        return RedirectToPage(new { Search });
    }
}
