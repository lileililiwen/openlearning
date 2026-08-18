namespace OpenLearning.Notifications.Configuration;

/// <summary>
/// Enabled flags for optional notification channels, bound from the
/// "Messaging" configuration section.
/// </summary>
public class ChannelOptions
{
    public const string SectionName = "Messaging";

    public bool SmsEnabled { get; set; }

    public bool PushEnabled { get; set; }

    /// <summary>VAPID subject (mailto: or https URL) advertised to browsers.</summary>
    public string? VapidSubject { get; set; }

    /// <summary>Base64url VAPID public key.</summary>
    public string? VapidPublicKey { get; set; }

    /// <summary>Base64url VAPID private key. Never exposed to clients.</summary>
    public string? VapidPrivateKey { get; set; }
}
