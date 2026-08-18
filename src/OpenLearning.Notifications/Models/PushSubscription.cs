namespace OpenLearning.Notifications.Models;

/// <summary>
/// A browser web-push subscription owned by a user. Unique per
/// (UserId, Endpoint); an endpoint is removed when the push service reports
/// it expired (404/410).
/// </summary>
public class PushSubscription
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>The push service endpoint the browser registered.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Base64url client public key used for payload encryption.</summary>
    public string P256Dh { get; set; } = string.Empty;

    /// <summary>Base64url client auth secret used for payload encryption.</summary>
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
