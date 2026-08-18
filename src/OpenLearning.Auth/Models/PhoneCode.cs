namespace OpenLearning.Auth.Models;

/// <summary>
/// A one-time verification code for phone-number sign-in. Stored in the DB so
/// codes survive restarts; single-use with a short expiry.
/// </summary>
public class PhoneCode
{
    public int Id { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the code is consumed (single use).</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>Failed verification attempts; lockout after 5.</summary>
    public int Attempts { get; set; }
}
