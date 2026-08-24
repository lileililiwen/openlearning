using Microsoft.EntityFrameworkCore;
using OpenLearning.Mobile.Models;

namespace OpenLearning.Mobile.Services;

/// <summary>
/// Manages native push endpoints bound to a mobile device. Registering a new
/// endpoint for a device replaces the previous one; removing it or logging out
/// the device revokes it without affecting other devices. Endpoints rejected
/// permanently by a provider are disabled and not re-registered.
/// </summary>
public class MobilePushService
{
    private readonly DbContext _db;

    public MobilePushService(DbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Registers or replaces the push endpoint for a device. A permanently
    /// rejected endpoint is never re-registered.
    /// </summary>
    public async Task<(bool Ok, string? Error)> RegisterAsync(
        string userId, string deviceId, string pushToken, string provider)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return (false, "Device id is required.");
        }

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return (false, "Push token is required.");
        }

        var existing = await _db.Set<MobilePushDevice>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DeviceId == deviceId);
        if (existing is not null && existing.Status == MobilePushStatus.PermanentlyRejected)
        {
            return (false, "This device's push endpoint was permanently rejected.");
        }

        if (existing is not null)
        {
            existing.PushToken = pushToken.Trim();
            existing.Provider = string.IsNullOrWhiteSpace(provider) ? "unknown" : provider.Trim();
            existing.RevokedAt = null;
            existing.Status = MobilePushStatus.Active;
        }
        else
        {
            _db.Set<MobilePushDevice>().Add(new MobilePushDevice
            {
                UserId = userId,
                DeviceId = deviceId,
                PushToken = pushToken.Trim(),
                Provider = string.IsNullOrWhiteSpace(provider) ? "unknown" : provider.Trim(),
                Status = MobilePushStatus.Active,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Removes a device's push endpoint.</summary>
    public async Task<bool> RemoveAsync(string userId, string deviceId)
    {
        var push = await _db.Set<MobilePushDevice>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DeviceId == deviceId);
        if (push is null)
        {
            return false;
        }

        _db.Set<MobilePushDevice>().Remove(push);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Marks a device's endpoint as permanently rejected by the provider so it
    /// is not re-registered.
    /// </summary>
    public async Task<bool> MarkPermanentlyRejectedAsync(string userId, string deviceId)
    {
        var push = await _db.Set<MobilePushDevice>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DeviceId == deviceId);
        if (push is null)
        {
            return false;
        }

        push.Status = MobilePushStatus.PermanentlyRejected;
        push.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
