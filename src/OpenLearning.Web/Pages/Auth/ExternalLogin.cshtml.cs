using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenLearning.Web.Pages.Auth;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return RedirectToPage("/Auth/Login");
        }

        var redirectUrl = Url.Page("/Auth/ExternalLoginCallback", null, new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }
}
