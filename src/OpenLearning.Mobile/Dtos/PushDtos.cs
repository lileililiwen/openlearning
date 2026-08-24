namespace OpenLearning.Mobile.Dtos;

/// <summary>Request to register or replace a native push endpoint for a device.</summary>
public sealed record MobilePushRegisterRequest(
    string DeviceId,
    string PushToken,
    string Provider);

/// <summary>Request to remove a device's push endpoint.</summary>
public sealed record MobilePushRemoveRequest(
    string DeviceId);
