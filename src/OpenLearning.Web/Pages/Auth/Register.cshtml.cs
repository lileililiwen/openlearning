using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Services;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Web.Pages.Auth;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly AccountService _account;
    private readonly DbContext _db;

    public RegisterModel(AccountService account, DbContext db)
    {
        _account = account;
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // GET renders the empty registration form; the model is bound and validated on POST.
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (result, user) = await _account.RegisterAsync(Input.Email, Input.Password, Input.DisplayName);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        await SeedDefaultPreferencesAsync(user!.Id);
        return RedirectToPage("/Index");
    }

    /// <summary>Seeds one all-enabled preference row per notification type.</summary>
    private async Task SeedDefaultPreferencesAsync(string userId)
    {
        foreach (var type in Enum.GetValues<NotificationType>())
        {
            _db.Set<NotificationPreference>().Add(new NotificationPreference
            {
                UserId = userId,
                Type = type,
            });
        }

        await _db.SaveChangesAsync();
    }
}
