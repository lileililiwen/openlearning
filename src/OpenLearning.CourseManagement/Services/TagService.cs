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
}
