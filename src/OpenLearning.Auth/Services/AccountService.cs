using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Services;

public class AccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<(IdentityResult Result, ApplicationUser? User)> RegisterAsync(
        string email, string password, string displayName)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        await _userManager.AddToRoleAsync(user, Roles.Student);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return (IdentityResult.Success, user);
    }

    public Task<SignInResult> LoginAsync(string email, string password, bool rememberMe)
    {
        return _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    }

    public Task SignOutAsync()
    {
        return _signInManager.SignOutAsync();
    }

    /// <summary>Number of users holding the given role (used for admin KPIs).</summary>
    public async Task<int> CountUsersInRoleAsync(string role)
    {
        return (await _userManager.GetUsersInRoleAsync(role)).Count;
    }

    public async Task<List<ApplicationUser>> GetRecentSignupsAsync(int count)
    {
        return await _userManager.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
    }
}
