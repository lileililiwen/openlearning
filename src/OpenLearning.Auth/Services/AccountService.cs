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

    /// <summary>
    /// Signs in by phone number, creating a Student account when the phone is
    /// new. Existing accounts are found by their normalized phone number.
    /// </summary>
    public async Task<(bool Ok, string? Error)> SignInByPhoneAsync(string phoneNumber)
    {
        var normalized = PhoneCodeService.Normalize(phoneNumber);
        if (normalized.Length == 0)
        {
            return (false, "A phone number is required.");
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalized);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = $"phone-{normalized}",
                DisplayName = $"User {normalized[^4..]}",
                PhoneNumber = normalized,
                PhoneNumberConfirmed = true,
            };
            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                return (false, string.Join(" ", created.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, Roles.Student);
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return (true, null);
    }

    /// <summary>
    /// Signs in an external (OAuth) identity, linking by email or creating a
    /// Student account when the email is new.
    /// </summary>
    public async Task<(bool Ok, string? Error, ApplicationUser? User)> SignInByEmailAsync(
        string email, string displayName)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedEmail.Length == 0)
        {
            return (false, "The identity provider did not return an email.", null);
        }

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? normalizedEmail.Split('@')[0]
                    : displayName,
                EmailConfirmed = true,
            };
            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                return (false, string.Join(" ", created.Errors.Select(e => e.Description)), null);
            }

            await _userManager.AddToRoleAsync(user, Roles.Student);
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return (true, null, user);
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
