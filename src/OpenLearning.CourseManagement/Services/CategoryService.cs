using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

/// <summary>
/// Admin maintenance of the managed category vocabulary. Renaming a category
/// keeps <see cref="Course.Category"/> (a string) in sync with a single
/// UPDATE, so courses never hold stale names.
/// </summary>
public class CategoryService
{
    private readonly DbContext _db;

    public CategoryService(DbContext db)
    {
        _db = db;
    }

    public Task<List<Category>> GetActiveAsync()
    {
        return _db.Set<Category>().AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public Task<List<Category>> GetAllAsync()
    {
        return _db.Set<Category>().AsNoTracking()
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public Task<Category?> GetByIdAsync(int id)
    {
        return _db.Set<Category>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(bool Ok, string? Error)> CreateAsync(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 100)
        {
            return (false, "Category name is required (100 characters or fewer).");
        }

        var slug = Slugify(trimmed);
        if (await _db.Set<Category>().AnyAsync(c => c.Slug == slug))
        {
            return (false, "A category with this name already exists.");
        }

        var maxOrder = await _db.Set<Category>().AnyAsync()
            ? await _db.Set<Category>().MaxAsync(c => c.OrderIndex)
            : 0;

        _db.Set<Category>().Add(new Category
        {
            Name = trimmed,
            Slug = slug,
            OrderIndex = maxOrder + 1,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Renames a category and cascades the new name to every course using it.</summary>
    public async Task<(bool Ok, string? Error)> RenameAsync(int id, string newName)
    {
        var trimmed = newName?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 100)
        {
            return (false, "Category name is required (100 characters or fewer).");
        }

        var category = await _db.Set<Category>().FindAsync(id);
        if (category is null)
        {
            return (false, "Category not found.");
        }

        var slug = Slugify(trimmed);
        if (await _db.Set<Category>().AnyAsync(c => c.Slug == slug && c.Id != id))
        {
            return (false, "A category with this name already exists.");
        }

        var oldName = category.Name;
        category.Name = trimmed;
        category.Slug = slug;

        // Cascade the rename to course text values. Load + save keeps this
        // provider-agnostic (no relational ExecuteUpdate in module deps).
        var courses = await _db.Set<Course>()
            .Where(c => c.Category == oldName)
            .ToListAsync();
        foreach (var course in courses)
        {
            course.Category = trimmed;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetActiveAsync(int id, bool isActive)
    {
        var category = await _db.Set<Category>().FindAsync(id);
        if (category is null)
        {
            return (false, "Category not found.");
        }

        category.IsActive = isActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Slugify a category name (same rules as tags: lowercase, hyphen runs).</summary>
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
}
