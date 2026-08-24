namespace OpenLearning.Mobile.Dtos;

/// <summary>One downloadable asset in an offline manifest.</summary>
public sealed record OfflineAssetDto(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    DateTime? AccessExpiresAt);

/// <summary>An expiring offline manifest for a course.</summary>
public sealed record OfflineManifestDto(
    int ManifestId,
    int CourseId,
    DateTime ExpiresAt,
    IReadOnlyList<OfflineAssetDto> Assets);

/// <summary>Request to prepare a course for offline use.</summary>
public sealed record OfflineManifestRequest(
    int CourseId);
