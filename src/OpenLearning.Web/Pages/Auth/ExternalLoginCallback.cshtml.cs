using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Auth;

[AllowAnonymous]
public class ExternalLoginCallbackModel : PageModel
{
    private readonly AccountService _account;

    public ExternalLoginCallbackModel(AccountService account)
    {
        _account = account;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Extract the identity created by the OAuth provider.
        var info = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (info?.Principal is null)
        {
            TempData["Message"] = "External sign-in failed or was cancelled.";
            TempData["MessageType"] = "danger";
            return Page();
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Message"] = "The identity provider did not return an email address.";
            TempData["MessageType"] = "danger";
            return Page();
        }

        var (ok, error, _) = await _account.SignInByEmailAsync(email, name ?? string.Empty);
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
            return Page();
        }

        // The external cookie is no longer needed.
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        var localUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
        return LocalRedirect(localUrl);
    }
}
