using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Logging.Services;
using OpenLearning.Mobile.Models;

namespace OpenLearning.Mobile.Services;

/// <summary>
/// Issues and manages device-bound mobile sessions: short-lived access tokens,
/// rotating refresh tokens stored only as hashes, and token-family revocation
/// on detected reuse. Security events are audited through <see cref="LogService"/>.
/// </summary>
public class MobileSessionService
{
    /// <summary>Lifetime of the short-lived access token.</summary>
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Lifetime of a refresh token before it must be rotated again.</summary>
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly DbContext _db;
    private readonly LogService _log;

    public MobileSessionService(DbContext db, LogService log)
    {
        _db = db;
        _log = log;
    }

    /// <summary>
    /// Creates a new device session and returns a fresh access/refresh token pair.
    /// Any existing session for the same (user, device) is revoked first.
    /// </summary>
    public async Task<(MobileSessionResult? Result, string? Error)> CreateSessionAsync(
        string userId, string deviceId, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return (null, "Device id is required.");
        }

        var existing = await _db.Set<DeviceSession>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId && s.RevokedAt == null);
        if (existing is not null)
        {
            await RevokeFamilyAsync(existing.TokenFamilyId, "replaced");
        }

        var familyId = Guid.NewGuid().ToString("N");
        var refreshSecret = GenerateSecret();
        var accessToken = GenerateSecret();

        var session = new DeviceSession
        {
            UserId = userId,
            DeviceId = deviceId,
            DeviceName = deviceName ?? string.Empty,
            RefreshTokenHash = Hash(refreshSecret),
            TokenFamilyId = familyId,
            AccessTokenExpiresAt = DateTime.UtcNow.Add(AccessTokenLifetime),
        };
        _db.Set<DeviceSession>().Add(session);
        _db.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(refreshSecret),
            FamilyId = familyId,
            Rotation = 1,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync();

        return (new MobileSessionResult(accessToken, refreshSecret, session.AccessTokenExpiresAt), null);
    }

    /// <summary>
    /// Rotates a refresh token. If the presented token is not the current one for
    /// its family, the whole family is revoked and the security event audited.
    /// </summary>
    public async Task<(MobileSessionResult? Result, string? Error)> RotateAsync(
        string userId, string deviceId, string refreshSecret)
    {
        if (string.IsNullOrWhiteSpace(refreshSecret))
        {
            return (null, "Refresh token is required.");
        }

        var hash = Hash(refreshSecret);
        var token = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId);
        if (token is null)
        {
            return (null, "Invalid refresh token.");
        }

        // Reuse detection runs before any other rejection: presenting a token
        // that is no longer the current (highest rotation) one means it was
        // replayed, so the whole family is revoked and the event audited.
        var current = await _db.Set<RefreshToken>()
            .Where(t => t.FamilyId == token.FamilyId)
            .OrderByDescending(t => t.Rotation)
            .FirstOrDefaultAsync();
        if (current is null || current.Id != token.Id)
        {
            await RevokeFamilyAsync(token.FamilyId, "reuse");
            await _log.RecordAsync(
                userId, userId, "mobile.token.reuse", "DeviceSession",
                token.FamilyId, $"Device {deviceId} presented a rotated refresh token.", null);
            return (null, "Refresh token reuse detected; session revoked.");
        }

        if (token.Revoked || token.ExpiresAt <= DateTime.UtcNow)
        {
            return (null, "Refresh token is no longer valid.");
        }

        var session = await _db.Set<DeviceSession>()
            .FirstOrDefaultAsync(s => s.TokenFamilyId == token.FamilyId && s.RevokedAt == null);
        if (session is null || session.DeviceId != deviceId)
        {
            return (null, "Refresh token is no longer valid.");
        }

        // Rotate: revoke the current token and issue the next one.
        token.Revoked = true;
        var nextSecret = GenerateSecret();
        var nextRotation = token.Rotation + 1;
        _db.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(nextSecret),
            FamilyId = token.FamilyId,
            Rotation = nextRotation,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
        });

        var accessToken = GenerateSecret();
        session.RefreshTokenHash = Hash(nextSecret);
        session.AccessTokenExpiresAt = DateTime.UtcNow.Add(AccessTokenLifetime);
        await _db.SaveChangesAsync();

        return (new MobileSessionResult(accessToken, nextSecret, session.AccessTokenExpiresAt), null);
    }

    /// <summary>Revokes a device session and its push endpoint (device logout).</summary>
    public async Task<bool> LogoutAsync(string userId, string deviceId)
    {
        var session = await _db.Set<DeviceSession>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId && s.RevokedAt == null);
        if (session is null)
        {
            return false;
        }

        await RevokeFamilyAsync(session.TokenFamilyId, "logout");
        await RevokePushAsync(userId, deviceId);
        return true;
    }

    /// <summary>Revokes a specific device session remotely (e.g. from another device).</summary>
    public async Task<bool> RevokeDeviceAsync(string userId, string deviceId)
    {
        var session = await _db.Set<DeviceSession>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId && s.RevokedAt == null);
        if (session is null)
        {
            return false;
        }

        await RevokeFamilyAsync(session.TokenFamilyId, "remote");
        await RevokePushAsync(userId, deviceId);
        return true;
    }

    /// <summary>True when the access token (identified by its session) is still valid.</summary>
    public async Task<bool> IsAccessTokenValidAsync(string userId, string deviceId)
    {
        var session = await _db.Set<DeviceSession>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId && s.RevokedAt == null);
        return session is not null && session.AccessTokenExpiresAt > DateTime.UtcNow;
    }

    private async Task RevokeFamilyAsync(string familyId, string reason)
    {
        var tokens = await _db.Set<RefreshToken>()
            .Where(t => t.FamilyId == familyId && !t.Revoked)
            .ToListAsync();
        foreach (var token in tokens)
        {
            token.Revoked = true;
        }

        var sessions = await _db.Set<DeviceSession>()
            .Where(s => s.TokenFamilyId == familyId && s.RevokedAt == null)
            .ToListAsync();
        foreach (var session in sessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
        }

        await _db.SaveChangesAsync();
    }

    private async Task RevokePushAsync(string userId, string deviceId)
    {
        var push = await _db.Set<MobilePushDevice>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DeviceId == deviceId && p.RevokedAt == null);
        if (push is not null)
        {
            push.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private static string GenerateSecret()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string Hash(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }
}

/// <summary>Result of creating or rotating a mobile session.</summary>
public sealed record MobileSessionResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);
