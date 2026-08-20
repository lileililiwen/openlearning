namespace OpenLearning.Storage.Models;

/// <summary>Metadata for one stored blob. Keys are server-generated and unique.</summary>
public class StoredFile
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string OriginalName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public FilePurpose Purpose { get; set; }

    /// <summary>True when the purpose is private (only the owner and admins may read it).</summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Admin-marked shared resource: visible to (and reusable by) every
    /// authenticated user. Never true for private purposes.
    /// </summary>
    public bool IsShared { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
