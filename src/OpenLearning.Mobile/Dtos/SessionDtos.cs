namespace OpenLearning.Mobile.Dtos;

/// <summary>Request to establish a device-bound mobile session.</summary>
public sealed record MobileSessionRequest(
    string DeviceId,
    string DeviceName);

/// <summary>Response containing the short-lived access token and rotating refresh token.</summary>
public sealed record MobileSessionResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);

/// <summary>Request to rotate a refresh token.</summary>
public sealed record MobileRefreshRequest(
    string DeviceId,
    string RefreshToken);

/// <summary>Request to log out a device (revokes its session and push endpoint).</summary>
public sealed record MobileLogoutRequest(
    string DeviceId);

/// <summary>Request to remotely revoke a specific device session.</summary>
public sealed record MobileRevokeRequest(
    string DeviceId);
