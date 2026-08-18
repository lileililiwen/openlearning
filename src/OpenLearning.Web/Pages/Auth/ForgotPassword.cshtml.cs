using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Models;
using OpenLearning.Notifications.Email;

namespace OpenLearning.Web.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender email,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _email = email;
        _config = config;
        _env = env;
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? DevResetLink { get; set; }

    public bool EmailEnabled { get; set; }

    public bool LinkShown => DevResetLink is not null;

    public async Task<IActionResult> OnPostAsync()
    {
        EmailEnabled = _config.GetValue<bool>("Email:Enabled");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            // Never reveal whether an account exists.
            return RedirectToPage("/Auth/ForgotPasswordConfirmation");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = Url.Page("/Auth/ResetPassword", null, new { email = user.Email, token }, Request.Scheme);

        if (EmailEnabled)
        {
            try
            {
                await _email.SendAsync(
                    user.Email!,
                    "[OpenLearning] Reset your password",
                    $"Reset your password by clicking: {resetLink}");
            }
            catch
            {
                // Email is best-effort; still show the confirmation page.
            }
        }
        else if (_env.IsDevelopment())
        {
            // Dev-only fallback: no email provider configured, so surface the link
            // on-screen to keep the flow testable. Never shown in production.
            DevResetLink = resetLink;
            return Page();
        }

        return RedirectToPage("/Auth/ForgotPasswordConfirmation");
    }
}
