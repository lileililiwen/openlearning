namespace OpenLearning.Mobile.Models;

/// <summary>Lifecycle state of a native push device endpoint.</summary>
public enum MobilePushStatus
{
    Active = 0,

    /// <summary>The provider permanently rejected the endpoint; it must not be re-registered.</summary>
    PermanentlyRejected = 1,
}

/// <summary>
/// A native push endpoint bound to a mobile device. Registering a new endpoint
/// for the same device replaces the previous one; removing it or logging out
/// the device revokes it without affecting other devices.
/// </summary>
public class MobilePushDevice
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Device identifier matching <see cref="DeviceSession.DeviceId"/>.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Provider token/endpoint (e.g. APNs device token or FCM registration token).</summary>
    public string PushToken { get; set; } = string.Empty;

    /// <summary>Push provider, e.g. "apns" or "fcm".</summary>
    public string Provider { get; set; } = string.Empty;

    public MobilePushStatus Status { get; set; } = MobilePushStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }
}
