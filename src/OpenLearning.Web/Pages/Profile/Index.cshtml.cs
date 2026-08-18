using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Profile;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ProfileService _profiles;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ProfileService profiles, UserManager<ApplicationUser> userManager)
    {
        _profiles = profiles;
        _userManager = userManager;
    }

    public class ProfileInputModel
    {
        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(500)]
        [Url(ErrorMessage = "Avatar must be a valid URL.")]
        public string? AvatarUrl { get; set; }
    }

    public class PasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public ApplicationUser? CurrentUser { get; set; }

    [BindProperty]
    public ProfileInputModel ProfileInput { get; set; } = new();

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        CurrentUser = user;
        if (user is not null)
        {
            ProfileInput.DisplayName = user.DisplayName;
            ProfileInput.Bio = user.Bio;
            ProfileInput.AvatarUrl = user.AvatarUrl;
        }
    }

    /// <summary>Roles held by the signed-in user, for the profile badge row.</summary>
    public IEnumerable<string> UserClaims()
    {
        var roles = new List<string>();
        if (User.IsInRole(Roles.Student))
            roles.Add("Student");
        if (User.IsInRole(Roles.Instructor))
            roles.Add("Instructor");
        if (User.IsInRole(Roles.Admin))
            roles.Add("Admin");
        return roles;
    }

    public async Task<IActionResult> OnPostSaveProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _profiles.UpdateProfileAsync(
            userId, ProfileInput.DisplayName, ProfileInput.Bio, ProfileInput.AvatarUrl);
        Flash(ok, error);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _profiles.ChangePasswordAsync(
            userId, PasswordInput.CurrentPassword, PasswordInput.NewPassword);
        Flash(ok, error);
        return RedirectToPage();
    }

    private void Flash(bool ok, string? error)
    {
        TempData["Message"] = ok ? "Saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
