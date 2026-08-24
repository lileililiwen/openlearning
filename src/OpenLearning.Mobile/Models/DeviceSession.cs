namespace OpenLearning.Mobile.Models;

/// <summary>
/// A device-bound mobile session. Access tokens are short-lived and stateless;
/// refresh tokens are stored only as hashes and rotate on every use.
/// </summary>
public class DeviceSession
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Client-supplied opaque device identifier (stable across reinstalls).</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Human-readable device label, e.g. "iPhone 15".</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the current refresh token (never the raw secret).</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>Groups all refresh tokens issued to this device; revoked together on reuse.</summary>
    public string TokenFamilyId { get; set; } = string.Empty;

    /// <summary>When the current access token expires; the client must refresh.</summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    /// <summary>Why the session was revoked: "logout", "reuse", "remote", "expired".</summary>
    public string? RevokedReason { get; set; }
}
