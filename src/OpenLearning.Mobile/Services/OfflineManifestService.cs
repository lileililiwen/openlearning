using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Mobile.Dtos;
using OpenLearning.Mobile.Models;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using OpenLearning.StudyTools.Models;

namespace OpenLearning.Mobile.Services;

/// <summary>
/// Prepares expiring offline manifests for downloadable content a learner can
/// currently access, and re-authorizes each asset download against the live
/// enrollment/access state (so access expiry is enforced even when a manifest
/// has not yet expired).
/// </summary>
public class OfflineManifestService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollment;
    private readonly StorageService _storage;

    public OfflineManifestService(DbContext db, EnrollmentService enrollment, StorageService storage)
    {
        _db = db;
        _enrollment = enrollment;
        _storage = storage;
    }

    /// <summary>Lifetime of a prepared offline manifest.</summary>
    public static readonly TimeSpan ManifestLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Creates an expiring manifest for a course the learner can currently
    /// access. Returns null when the learner has no active access.
    /// </summary>
    public async Task<(OfflineManifestDto? Manifest, string? Error)> CreateManifestAsync(
        string userId, int courseId)
    {
        if (!await _enrollment.IsEnrolledAsync(userId, courseId))
        {
            return (null, "You are not enrolled in this course.");
        }

        if (await _enrollment.IsAccessExpiredAsync(userId, courseId))
        {
            return (null, "Your access to this course has expired.");
        }

        var accessExpiresAt = await _db.Set<OpenLearning.Enrollment.Models.Enrollment>()
            .AsNoTracking()
            .Where(e => e.StudentId == userId && e.CourseId == courseId && e.RevokedAt == null)
            .Select(e => e.AccessExpiresAt)
            .FirstOrDefaultAsync();

        var lessonIds = await _db.Set<Lesson>()
            .Where(l => l.Module!.CourseId == courseId)
            .Select(l => l.Id)
            .ToListAsync();

        var downloads = await _db.Set<LessonDownload>().AsNoTracking()
            .Where(d => d.IsAllowed && lessonIds.Contains(d.LessonId))
            .ToListAsync();

        var assets = new List<OfflineManifestAsset>();
        foreach (var download in downloads)
        {
            var storageKey = KeyFromUrl(download.FileUrl);
            if (storageKey is null)
            {
                continue;
            }

            var stored = await _storage.GetAsync(storageKey);
            if (stored is null)
            {
                continue;
            }

            assets.Add(new OfflineManifestAsset
            {
                StorageKey = stored.Key,
                FileName = stored.OriginalName,
                ContentType = stored.ContentType,
                SizeBytes = stored.SizeBytes,
                Checksum = await ComputeChecksumAsync(stored.Key),
                AccessExpiresAt = accessExpiresAt,
            });
        }

        var manifest = new OfflineManifest
        {
            UserId = userId,
            CourseId = courseId,
            ExpiresAt = DateTime.UtcNow.Add(ManifestLifetime),
            Assets = assets,
        };
        _db.Set<OfflineManifest>().Add(manifest);
        await _db.SaveChangesAsync();

        return (ToDto(manifest), null);
    }

    /// <summary>
    /// Re-authorizes a single asset download. Access is denied when the learner
    /// is no longer enrolled, access has expired, or the manifest has expired —
    /// even if the manifest itself has not yet expired.
    /// </summary>
    public async Task<(bool Ok, string? Error)> AuthorizeAssetAsync(
        string userId, int manifestId, string storageKey)
    {
        var manifest = await _db.Set<OfflineManifest>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == manifestId && m.UserId == userId);
        if (manifest is null)
        {
            return (false, "Manifest not found.");
        }

        if (manifest.ExpiresAt <= DateTime.UtcNow)
        {
            return (false, "Manifest has expired.");
        }

        var asset = await _db.Set<OfflineManifestAsset>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.ManifestId == manifestId && a.StorageKey == storageKey);
        if (asset is null)
        {
            return (false, "Asset not in manifest.");
        }

        if (!await _enrollment.IsEnrolledAsync(userId, manifest.CourseId))
        {
            return (false, "Your enrollment is no longer active.");
        }

        if (await _enrollment.IsAccessExpiredAsync(userId, manifest.CourseId))
        {
            return (false, "Your access to this course has expired.");
        }

        if (asset.AccessExpiresAt is DateTime deadline && DateTime.UtcNow > deadline)
        {
            return (false, "Your access to this course has expired.");
        }

        return (true, null);
    }

    private async Task<string> ComputeChecksumAsync(string key)
    {
        var (_, stream) = await _storage.OpenAsync(key);
        if (stream is null)
        {
            return string.Empty;
        }

        using (stream)
        using (var sha = SHA256.Create())
        {
            var hash = await sha.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }
    }

    private static string? KeyFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var marker = "/files/";
        var idx = url.IndexOf(marker, StringComparison.Ordinal);
        return idx < 0 ? null : url[(idx + marker.Length)..];
    }

    private static OfflineManifestDto ToDto(OfflineManifest manifest)
    {
        return new OfflineManifestDto(
            manifest.Id,
            manifest.CourseId,
            manifest.ExpiresAt,
            manifest.Assets
                .Select(a => new OfflineAssetDto(
                    a.StorageKey, a.FileName, a.ContentType, a.SizeBytes, a.Checksum, a.AccessExpiresAt))
                .ToList());
    }
}
