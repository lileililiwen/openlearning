using Microsoft.EntityFrameworkCore;
using OpenLearning.Storage.Models;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Storage.Services;

/// <summary>
/// Validates, stores, serves, and deletes blobs with metadata. Keys are
/// server-generated (<c>{purpose}/{guid}{ext}</c>) so user input never enters
/// the path. Videos are enqueued for rendition generation.
/// </summary>
public class StorageService
{
    /// <summary>Per-purpose defaults; system-config can override these later.</summary>
    private static readonly Dictionary<FilePurpose, (long MaxBytes, string[] Extensions)> _limits =
        new()
        {
            [FilePurpose.Avatar] = (2 * 1024 * 1024, new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }),
            [FilePurpose.Video] = (500L * 1024 * 1024, new[] { ".mp4", ".webm", ".mov" }),
            [FilePurpose.Courseware] = (100L * 1024 * 1024, new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip" }),
            [FilePurpose.Assignment] = (50L * 1024 * 1024, new[] { ".pdf", ".doc", ".docx", ".zip" }),
            [FilePurpose.Answer] = (25L * 1024 * 1024, new[] { ".pdf", ".doc", ".docx", ".zip", ".jpg", ".jpeg", ".png" }),
            [FilePurpose.AsyncIO] = (100L * 1024 * 1024, new[] { ".csv", ".xlsx", ".xls", ".json", ".txt", ".zip", ".pdf", ".doc", ".docx" }),
        };

    private readonly DbContext _db;
    private readonly IStorageProvider _storage;
    private readonly MediaTranscoder _transcoder;
    private readonly SystemConfigService? _config;

    public StorageService(
        DbContext db,
        IStorageProvider storage,
        MediaTranscoder transcoder,
        SystemConfigService? config = null)
    {
        _db = db;
        _storage = storage;
        _transcoder = transcoder;
        _config = config;
    }

    public static (long MaxBytes, string[] Extensions) GetLimits(FilePurpose purpose)
    {
        return _limits[purpose];
    }

    /// <summary>
    /// Effective per-purpose limits: system-config overrides
    /// (<c>Storage.Limits.&lt;Purpose&gt;.MaxBytes</c> /
    /// <c>.Extensions</c>) with the static defaults as fallback.
    /// </summary>
    public async Task<(long MaxBytes, string[] Extensions)> GetLimitsAsync(FilePurpose purpose)
    {
        var (defaultMax, defaultExtensions) = _limits[purpose];
        if (_config is null)
        {
            return (defaultMax, defaultExtensions);
        }

        var maxBytes = await _config.GetIntAsync($"Storage.Limits.{purpose}.MaxBytes", (int)defaultMax);
        var extensionsCsv = await _config.GetStringAsync(
            $"Storage.Limits.{purpose}.Extensions", string.Join(",", defaultExtensions));
        var extensions = extensionsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.StartsWith('.') ? ext : "." + ext)
            .ToArray();
        return (maxBytes, extensions.Length == 0 ? defaultExtensions : extensions);
    }

    public static bool IsPrivatePurpose(FilePurpose purpose)
    {
        return purpose == FilePurpose.Answer;
    }

    public async Task<(StoredFile? File, string? Error)> UploadAsync(
        string ownerId, FilePurpose purpose, string fileName, string contentType, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return (null, "File name is required.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var (maxBytes, allowedExtensions) = await GetLimitsAsync(purpose);
        if (!allowedExtensions.Contains(extension))
        {
            return (null, $"File type '{extension}' is not allowed for {purpose}.");
        }

        if (stream.Length == 0)
        {
            return (null, "File is empty.");
        }

        if (stream.Length > maxBytes)
        {
            return (null, $"File exceeds the {maxBytes / (1024 * 1024)} MB limit for {purpose}.");
        }

        var key = $"{purpose.ToString().ToLowerInvariant()}/{Guid.NewGuid():N}{extension}";
        await _storage.SaveAsync(stream, key);

        var file = new StoredFile
        {
            Key = key,
            OriginalName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = stream.Length,
            OwnerId = ownerId,
            Purpose = purpose,
            IsPrivate = IsPrivatePurpose(purpose),
        };
        _db.Set<StoredFile>().Add(file);
        await _db.SaveChangesAsync();

        if (purpose == FilePurpose.Video)
        {
            _db.Set<MediaAsset>().Add(new MediaAsset { StoredFileId = file.Id });
            await _db.SaveChangesAsync();
            _transcoder.Enqueue(new TranscodeRequest(file.Id, key));
        }

        return (file, null);
    }

    public Task<StoredFile?> GetAsync(string key)
    {
        return _db.Set<StoredFile>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == key);
    }

    public Task<StoredFile?> GetByIdAsync(int id)
    {
        return _db.Set<StoredFile>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<(StoredFile? File, Stream? Stream)> OpenAsync(string key)
    {
        var file = await GetAsync(key);
        var stream = await _storage.OpenAsync(key);
        return (file, stream);
    }

    public async Task<bool> DeleteAsync(string key, string requesterId, bool isAdmin)
    {
        var file = await _db.Set<StoredFile>().FirstOrDefaultAsync(f => f.Key == key);
        if (file is null)
        {
            return false;
        }

        if (!isAdmin && file.OwnerId != requesterId)
        {
            return false;
        }

        await _storage.DeleteAsync(key);
        if (file.Purpose == FilePurpose.Video)
        {
            var baseKey = key[..key.LastIndexOf('.')];
            foreach (var name in new[] { "low", "mid", "high" })
            {
                await _storage.DeleteAsync($"{baseKey}.{name}.mp4");
            }
        }

        _db.Set<StoredFile>().Remove(file);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Rendition state for a video, or null when the file is not a video.</summary>
    public async Task<MediaAsset?> GetRenditionsAsync(string key)
    {
        var file = await GetAsync(key);
        if (file is null)
        {
            return null;
        }

        return await _db.Set<MediaAsset>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.StoredFileId == file.Id);
    }

    /// <summary>Rendition state by stored-file id (used by the renditions endpoint).</summary>
    public Task<MediaAsset?> GetRenditionsByIdAsync(int storedFileId)
    {
        return _db.Set<MediaAsset>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.StoredFileId == storedFileId);
    }
}
