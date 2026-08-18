using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

/// <summary>
/// Maintains the flat tag vocabulary: lists active tags and ensures names
/// exist (auto-creating unknown names with a de-duplicated slug).
/// </summary>
public class TagService
{
    private readonly DbContext _db;

    public TagService(DbContext db)
    {
        _db = db;
    }

    public Task<List<Tag>> GetActiveAsync()
    {
        return _db.Set<Tag>().AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public Task<List<Tag>> GetAllAsync()
    {
        return _db.Set<Tag>().AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>Course counts for every tag id in a single grouped query.</summary>
    public async Task<Dictionary<int, int>> GetCourseCountsAsync()
    {
        return await _db.Set<CourseTag>().AsNoTracking()
            .GroupBy(ct => ct.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.TagId, g => g.Count);
    }

    /// <summary>Slugify a tag name: lowercase, non-alphanumeric runs become hyphens.</summary>
    public static string Slugify(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var lastWasHyphen = false;
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    /// <summary>
    /// Resolves tag names to their entities, creating any unknown names on
    /// the fly. Returns the set of (possibly new) tags.
    /// </summary>
    public async Task<List<Tag>> EnsureByNamesAsync(IEnumerable<string> names)
    {
        var trimmed = names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (trimmed.Count == 0)
        {
            return new List<Tag>();
        }

        var slugs = trimmed.Select(Slugify).Where(s => s.Length > 0).Distinct().ToList();
        var existing = await _db.Set<Tag>().AsNoTracking()
            .Where(t => slugs.Contains(t.Slug))
            .ToDictionaryAsync(t => t.Slug);

        var result = new List<Tag>();
        var toAdd = new List<Tag>();
        foreach (var name in trimmed)
        {
            var slug = Slugify(name);
            if (slug.Length == 0)
            {
                continue;
            }

            if (existing.TryGetValue(slug, out var tag))
            {
                result.Add(tag);
                continue;
            }

            var created = new Tag { Name = name, Slug = slug, IsActive = true };
            toAdd.Add(created);
            result.Add(created);
        }

        if (toAdd.Count > 0)
        {
            _db.Set<Tag>().AddRange(toAdd);
            await _db.SaveChangesAsync();
        }

        return result;
    }

    /// <summary>Renames a tag, keeping its slug (and therefore its URL) stable.</summary>
    public async Task<(bool Ok, string? Error)> RenameAsync(int tagId, string newName)
    {
        var trimmed = newName?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 100)
        {
            return (false, "Tag name is required (100 characters or fewer).");
        }

        var tag = await _db.Set<Tag>().FindAsync(tagId);
        if (tag is null)
        {
            return (false, "Tag not found.");
        }

        tag.Name = trimmed;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Merges the source tag into the target: re-points joins, then deletes the source.</summary>
    public async Task<(bool Ok, string? Error)> MergeAsync(int fromTagId, int toTagId)
    {
        if (fromTagId == toTagId)
        {
            return (false, "Cannot merge a tag into itself.");
        }

        var from = await _db.Set<Tag>().FindAsync(fromTagId);
        var to = await _db.Set<Tag>().FindAsync(toTagId);
        if (from is null || to is null)
        {
            return (false, "Tag not found.");
        }

        // TagId is part of the composite key, so joins cannot be edited in
        // place; delete the source joins and re-create them under the target.
        var joins = await _db.Set<CourseTag>()
            .Where(ct => ct.TagId == fromTagId)
            .ToListAsync();
        foreach (var join in joins)
        {
            var duplicate = await _db.Set<CourseTag>()
                .FirstOrDefaultAsync(ct => ct.CourseId == join.CourseId && ct.TagId == toTagId);
            if (duplicate is not null)
            {
                _db.Set<CourseTag>().Remove(join);
                continue;
            }

            var courseId = join.CourseId;
            _db.Set<CourseTag>().Remove(join);
            _db.Set<CourseTag>().Add(new CourseTag { CourseId = courseId, TagId = toTagId });
        }

        _db.Set<Tag>().Remove(from);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Retires a tag: hidden from forms/filters but kept on existing courses.</summary>
    public async Task<(bool Ok, string? Error)> RetireAsync(int tagId)
    {
        var tag = await _db.Set<Tag>().FindAsync(tagId);
        if (tag is null)
        {
            return (false, "Tag not found.");
        }

        tag.IsActive = false;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
