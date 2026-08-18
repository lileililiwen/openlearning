using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

/// <summary>One page of catalog results plus the total for pagination.</summary>
public sealed record CourseSearchResult(IReadOnlyList<Course> Courses, int TotalCount);

public enum CourseSort
{
    Newest = 0,
    Popular = 1,
    PriceAsc = 2,
    PriceDesc = 3,
    Rating = 4,
}

public class CourseService
{
    private readonly DbContext _db;
    private readonly TagService _tags;

    public CourseService(DbContext db, TagService tags)
    {
        _db = db;
        _tags = tags;
    }

    public Task<List<Course>> GetPublishedCoursesAsync()
    {
        return _db.Set<Course>().AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.Tags).ThenInclude(t => t.Tag)
                .Where(c => c.Status == CourseStatus.Published)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
    }

    public Task<List<Course>> GetByInstructorAsync(string instructorId)
    {
        return _db.Set<Course>().AsNoTracking()
                .Include(c => c.Modules).ThenInclude(m => m.Lessons)
                .Include(c => c.Tags).ThenInclude(t => t.Tag)
                .Where(c => c.InstructorId == instructorId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
    }

    public Task<List<Course>> GetAllAsync()
    {
        return _db.Set<Course>().AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.Tags).ThenInclude(t => t.Tag)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
    }

    public Task<Course?> GetByIdAsync(int id)
    {
        return _db.Set<Course>().AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.Tags).ThenInclude(t => t.Tag)
                .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                    .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
                .FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<bool> IsOwnerAsync(int courseId, string userId)
    {
        return _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    public async Task<Course?> CreateAsync(
        string instructorId,
        string title,
        string description,
        string category,
        decimal? price,
        CourseLevel? level,
        string duration,
        string language,
        string prerequisites,
        string learningOutcomes,
        IEnumerable<string>? tagNames = null)
    {
        var course = new Course
        {
            Title = title,
            Description = description,
            Category = category,
            Price = price,
            Level = level,
            Duration = duration,
            Language = language,
            Prerequisites = prerequisites,
            LearningOutcomes = learningOutcomes,
            InstructorId = instructorId,
        };

        _db.Set<Course>().Add(course);
        await _db.SaveChangesAsync();
        await SetTagsAsync(course.Id, tagNames);
        return course;
    }

    public async Task<bool> UpdateAsync(
        int courseId,
        string ownerId,
        string title,
        string description,
        string category,
        decimal? price,
        CourseLevel? level,
        string duration,
        string language,
        string prerequisites,
        string learningOutcomes,
        IEnumerable<string>? tagNames = null)
    {
        var course = await _db.Set<Course>()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == ownerId);
        if (course is null)
        {
            return false;
        }

        course.Title = title;
        course.Description = description;
        course.Category = category;
        course.Price = price;
        course.Level = level;
        course.Duration = duration;
        course.Language = language;
        course.Prerequisites = prerequisites;
        course.LearningOutcomes = learningOutcomes;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await SetTagsAsync(courseId, tagNames);
        return true;
    }

    /// <summary>Replaces the course's tags with the resolved set (auto-creating unknown names).</summary>
    private async Task SetTagsAsync(int courseId, IEnumerable<string>? tagNames)
    {
        var tags = tagNames is null ? new List<Tag>() : await _tags.EnsureByNamesAsync(tagNames);
        var existing = await _db.Set<CourseTag>()
            .Where(ct => ct.CourseId == courseId)
            .ToListAsync();
        var desired = tags.Select(t => t.Id).ToHashSet();

        foreach (var link in existing.Where(ct => !desired.Contains(ct.TagId)))
        {
            _db.Set<CourseTag>().Remove(link);
        }

        foreach (var tag in tags.Where(t => !existing.Any(ct => ct.TagId == t.Id)))
        {
            _db.Set<CourseTag>().Add(new CourseTag { CourseId = courseId, TagId = tag.Id });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int courseId, string ownerId)
    {
        var course = await _db.Set<Course>()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == ownerId);
        if (course is null)
        {
            return false;
        }

        _db.Set<Course>().Remove(course);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Admin-only delete; bypasses ownership checks.</summary>
    public async Task<bool> DeleteAnyAsync(int courseId)
    {
        var course = await _db.Set<Course>().FindAsync(courseId);
        if (course is null)
        {
            return false;
        }

        _db.Set<Course>().Remove(course);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Ok, string? Error)> SetStatusAsync(int courseId, string ownerId, CourseStatus status)
    {
        var course = await _db.Set<Course>()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == ownerId);
        if (course is null)
        {
            return (false, "Course not found.");
        }

        // Instructors must be identity-verified before publishing a course.
        if (status == CourseStatus.Published)
        {
            var identityStatus = await _db.Set<ApplicationUser>()
                .Where(u => u.Id == ownerId)
                .Select(u => u.IdentityStatus)
                .FirstOrDefaultAsync();
            if (identityStatus != IdentityStatus.Verified)
            {
                return (false, "Your identity must be verified before you can publish a course.");
            }
        }

        course.Status = status;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<int> GetLessonCountAsync(int courseId)
    {
        return _db.Set<Module>().AsNoTracking()
                .Where(m => m.CourseId == courseId)
                .SelectMany(m => m.Lessons)
                .CountAsync();
    }

    /// <summary>
    /// Published courses matching any of the given categories, newest first,
    /// excluding the supplied course ids. Used for dashboard recommendations.
    /// </summary>
    public async Task<List<Course>> GetRecommendationsAsync(
        List<string> categories, List<int> excludeCourseIds, int count)
    {
        var normalized = categories.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (normalized.Count == 0)
        {
            return new List<Course>();
        }

        return await _db.Set<Course>().AsNoTracking()
            .Include(c => c.Instructor)
            .Where(c => c.Status == CourseStatus.Published
                && normalized.Contains(c.Category)
                && !excludeCourseIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(int Draft, int Published)> GetCourseCountsAsync()
    {
        var drafts = await _db.Set<Course>().CountAsync(c => c.Status == CourseStatus.Draft);
        var published = await _db.Set<Course>().CountAsync(c => c.Status == CourseStatus.Published);
        return (drafts, published);
    }

    public Task<List<Course>> GetRecentCoursesAsync(int count)
    {
        return _db.Set<Course>().AsNoTracking()
                .Include(c => c.Instructor)
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();
    }

    /// <summary>
    /// Paginated catalog search over published courses. Returns the page and
    /// the total count for the matched set (not the entire page).
    /// </summary>
    public async Task<CourseSearchResult> SearchAsync(
        string? search,
        string? category,
        string? tag,
        CourseSort sort,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 48);

        IQueryable<Course> query = _db.Set<Course>().AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            // CA1862 wants the StringComparison overload, but EF Core cannot translate it
            // to SQL; lowercasing both sides is the provider-agnostic translatable form.
#pragma warning disable CA1862
            query = query.Where(c =>
                c.Title.ToLowerInvariant().Contains(term)
                || c.Description.ToLowerInvariant().Contains(term)
                || c.Category.ToLowerInvariant().Contains(term));
#pragma warning restore CA1862
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            query = query.Where(c => c.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var slug = tag.Trim().ToLowerInvariant();
            // Resolve slug -> tag id, then project matching course ids. This
            // avoids navigation-based Any which is not reliably translated
            // when combined with pagination.
            var tagId = await _db.Set<Tag>().AsNoTracking()
                .Where(t => t.Slug == slug && t.IsActive)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
            if (tagId is null)
            {
                return new CourseSearchResult(new List<Course>(), 0);
            }

            var courseIds = await _db.Set<CourseTag>().AsNoTracking()
                .Where(ct => ct.TagId == tagId)
                .Select(ct => ct.CourseId)
                .ToListAsync();
            query = query.Where(c => courseIds.Contains(c.Id));
        }

        var total = await query.CountAsync();

        query = sort switch
        {
            CourseSort.PriceAsc => query
                .OrderBy(c => c.Price ?? 0m)
                .ThenByDescending(c => c.CreatedAt),
            CourseSort.PriceDesc => query
                .OrderByDescending(c => c.Price ?? 0m)
                .ThenByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt),
        };

        var courses = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Instructor)
            .Include(c => c.Tags).ThenInclude(t => t.Tag)
            .ToListAsync();

        return new CourseSearchResult(courses, total);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _db.Set<Category>().AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();
    }
}
