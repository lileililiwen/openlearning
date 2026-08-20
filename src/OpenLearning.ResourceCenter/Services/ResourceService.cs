using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.ResourceCenter.Services;

/// <summary>One resource-library row with its owner display name and served URL.</summary>
public sealed record ResourceRow(StoredFile File, string OwnerName, string Url);

/// <summary>
/// The resource center: a per-user/admin library over <see cref="StoredFile"/>.
/// Users see their own uploads plus admin-shared resources; admins see
/// everything and can share/delete any resource.
/// </summary>
public class ResourceService
{
    public const int PageSize = 24;

    private readonly DbContext _db;
    private readonly StorageService _storage;

    public ResourceService(DbContext db, StorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<(List<ResourceRow> Items, int Total)> ListAsync(
        string userId, bool isAdmin, FilePurpose? purpose, string? search, int page)
    {
        IQueryable<StoredFile> query = _db.Set<StoredFile>().AsNoTracking();
        if (!isAdmin)
        {
            query = query.Where(f => f.OwnerId == userId || f.IsShared);
        }

        if (purpose is not null)
        {
            query = query.Where(f => f.Purpose == purpose);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f => f.OriginalName.Contains(term));
        }

        var total = await query.CountAsync();
        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var ownerIds = files.Select(f => f.OwnerId).Distinct().ToList();
        var ownerNames = new Dictionary<string, string>();
        if (ownerIds.Count > 0)
        {
            ownerNames = await _db.Set<ApplicationUser>().AsNoTracking()
                .Where(u => ownerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName);
        }

        var items = files
            .Select(f => new ResourceRow(
                f,
                ownerNames.GetValueOrDefault(f.OwnerId) ?? f.OwnerId,
                $"/files/{f.Key}"))
            .ToList();
        return (items, total);
    }

    /// <summary>Uploads an image/video/document into the resource center.</summary>
    public async Task<(StoredFile? File, string? Error)> UploadAsync(string userId, FilePurpose purpose, IFormFile file)
    {
        if (purpose is not (FilePurpose.Image or FilePurpose.Video or FilePurpose.Document))
        {
            return (null, "资源中心仅支持图片、视频或文档。");
        }

        await using var stream = file.OpenReadStream();
        return await _storage.UploadAsync(userId, purpose, file.FileName, file.ContentType, stream);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(string key, string userId, bool isAdmin)
    {
        var file = await _db.Set<StoredFile>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == key);
        if (file is null)
        {
            return (false, "资源不存在。");
        }

        if (!isAdmin && file.OwnerId != userId)
        {
            return (false, "无权删除该资源。");
        }

        return await _storage.DeleteAsync(key, userId, isAdmin)
            ? (true, null)
            : (false, "删除失败。");
    }

    public async Task<(bool Ok, string? Error)> SetSharedAsync(string key, string userId, bool isAdmin, bool shared)
    {
        if (!isAdmin)
        {
            return (false, "仅管理员可以共享资源。");
        }

        var file = await _db.Set<StoredFile>().FirstOrDefaultAsync(f => f.Key == key);
        if (file is null)
        {
            return (false, "资源不存在。");
        }

        if (shared && file.IsPrivate)
        {
            return (false, "私有用途的资源不能共享。");
        }

        file.IsShared = shared;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<StoredFile?> GetByIdAsync(int id)
    {
        return _db.Set<StoredFile>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public Task<StoredFile?> GetByKeyAsync(string key)
    {
        return _db.Set<StoredFile>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == key);
    }
}
