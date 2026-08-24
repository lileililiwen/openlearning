namespace OpenLearning.Mobile.Models;

/// <summary>
/// One downloadable asset within an <see cref="OfflineManifest"/>. The access
/// expiry is captured at manifest creation so a later check can deny an asset
/// even when the manifest has not yet expired.
/// </summary>
public class OfflineManifestAsset
{
    public int Id { get; set; }

    public int ManifestId { get; set; }

    public OfflineManifest? Manifest { get; set; }

    /// <summary>Storage key of the underlying blob.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>SHA-256 checksum (hex) of the blob for download verification.</summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>Course access expiry at manifest creation; null = unlimited.</summary>
    public DateTime? AccessExpiresAt { get; set; }
}
