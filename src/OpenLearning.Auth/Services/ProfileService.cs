using Microsoft.AspNetCore.Identity;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Services;

public class ProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Updates display name, bio, and avatar for the signed-in user.</summary>
    public async Task<(bool Ok, string? Error)> UpdateProfileAsync(
        string userId, string? displayName, string? bio, string? avatarUrl)
    {
        var trimmedName = displayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (false, "Display name is required.");
        }

        var trimmedBio = bio?.Trim() ?? string.Empty;
        if (trimmedBio.Length > 2000)
        {
            return (false, "Bio must be 2000 characters or fewer.");
        }

        var trimmedAvatar = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        if (trimmedAvatar is { Length: > 500 })
        {
            return (false, "Avatar URL must be 500 characters or fewer.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        user.DisplayName = trimmedName;
        user.Bio = trimmedBio;
        user.AvatarUrl = trimmedAvatar;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, null) : (false, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>Changes the password, verifying the current password first.</summary>
    public async Task<(bool Ok, string? Error)> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }
}
