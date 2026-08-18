using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AccountService _account;

    public LoginModel(AccountService account)
    {
        _account = account;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    /// <summary>Configured external authentication schemes (Google/GitHub).</summary>
    public List<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalProviders { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
        var identitySchemes = new[]
        {
            IdentityConstants.ApplicationScheme,
            IdentityConstants.ExternalScheme,
            IdentityConstants.TwoFactorRememberMeScheme,
            IdentityConstants.TwoFactorUserIdScheme,
        };
        ExternalProviders = (await HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>()
                .GetAllSchemesAsync())
            .Where(s => !identitySchemes.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _account.LoginAsync(Input.Email, Input.Password, Input.RememberMe);
        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }
}
