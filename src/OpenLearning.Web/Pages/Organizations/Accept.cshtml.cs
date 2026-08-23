using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Models;
using OpenLearning.Organizations.Services;

namespace OpenLearning.Web.Pages.Organizations;

[Authorize]
public sealed class AcceptModel(OrganizationService organizations, UserManager<ApplicationUser> users) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Token { get; set; } = string.Empty;
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user?.Email is null)
        {
            return Forbid();
        }

        if (!await organizations.AcceptInvitationAsync(Token, user.Id, user.Email))
        {
            TempData["Message"] = "Invitation is invalid, expired, or belongs to another email.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { Token });
        }

        return RedirectToPage("Switch");
    }
}
