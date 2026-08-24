namespace OpenLearning.Mobile.Models;

/// <summary>
/// An expiring offline manifest describing downloadable content currently
/// accessible to a learner. Assets carry checksums and sizes so clients can
/// verify resumable downloads.
/// </summary>
public class OfflineManifest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    /// <summary>When the manifest itself stops being valid for new downloads.</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OfflineManifestAsset> Assets { get; set; } = new List<OfflineManifestAsset>();
}
