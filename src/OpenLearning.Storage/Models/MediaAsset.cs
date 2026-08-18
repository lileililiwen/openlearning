namespace OpenLearning.Storage.Models;

/// <summary>Rendition state for an uploaded video, consumed by the player.</summary>
public class MediaAsset
{
    public int Id { get; set; }

    public int StoredFileId { get; set; }

    public StoredFile? StoredFile { get; set; }

    public RenditionStatus Status { get; set; } = RenditionStatus.Pending;

    public string? LowUrl { get; set; }

    public string? MidUrl { get; set; }

    public string? HighUrl { get; set; }

    public string? Error { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
