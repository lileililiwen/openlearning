using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// Submits a real-name verification request. The identity number is stored
    /// hashed (SHA-256); the raw value is never persisted. Rejects re-submission
    /// while a request is already pending.
    /// </summary>
    public async Task<(bool Ok, string? Error)> SubmitVerificationAsync(
        string userId,
        string? realName,
        IdType idType,
        string? idNumber,
        string? documentUrl)
    {
        var trimmedName = realName?.Trim() ?? string.Empty;
        if (trimmedName.Length is 0 or > 200)
        {
            return (false, "Real name is required (200 characters or fewer).");
        }

        var trimmedNumber = idNumber?.Trim() ?? string.Empty;
        if (trimmedNumber.Length is 0 or > 100)
        {
            return (false, "Identity number is required (100 characters or fewer).");
        }

        var trimmedDoc = string.IsNullOrWhiteSpace(documentUrl) ? null : documentUrl.Trim();
        if (trimmedDoc is { Length: > 500 })
        {
            return (false, "Document URL must be 500 characters or fewer.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        if (user.IdentityStatus == IdentityStatus.Pending)
        {
            return (false, "A verification request is already pending review.");
        }

        user.RealName = trimmedName;
        user.IdType = idType;
        user.IdNumberHash = HashIdNumber(trimmedNumber);
        user.VerificationDocumentUrl = trimmedDoc;
        user.IdentityStatus = IdentityStatus.Pending;
        user.VerifiedAt = null;
        user.VerificationNote = null;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, null) : (false, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>SHA-256 hex digest of the identity number; the raw value is never stored.</summary>
    public static string HashIdNumber(string idNumber)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idNumber)));
    }
}
