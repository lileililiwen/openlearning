namespace OpenLearning.Mobile.Models;

/// <summary>
/// A single rotated refresh token in a device's token family. Only the hash is
/// stored; presenting a token that is no longer the current one indicates reuse
/// and revokes the whole family.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the refresh token secret.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Token family this token belongs to (see <see cref="DeviceSession.TokenFamilyId"/>).</summary>
    public string FamilyId { get; set; } = string.Empty;

    /// <summary>Monotonic rotation counter; the highest is the current token.</summary>
    public int Rotation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    /// <summary>True once this token has been rotated past (superseded) or the family revoked.</summary>
    public bool Revoked { get; set; }
}
