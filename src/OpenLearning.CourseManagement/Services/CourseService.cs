using Microsoft.EntityFrameworkCore;
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

    public CourseService(DbContext db)
    {
        _db = db;
    }

    public Task<List<Course>> GetPublishedCoursesAsync()
        => _db.Set<Course>().AsNoTracking()
            .Include(c => c.Instructor)
            .Where(c => c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public Task<List<Course>> GetByInstructorAsync(string instructorId)
        => _db.Set<Course>().AsNoTracking()
            .Include(c => c.Modules).ThenInclude(m => m.Lessons)
            .Where(c => c.InstructorId == instructorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public Task<List<Course>> GetAllAsync()
        => _db.Set<Course>().AsNoTracking()
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public Task<Course?> GetByIdAsync(int id)
        => _db.Set<Course>().AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> IsOwnerAsync(int courseId, string userId)
        => _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);

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
        string learningOutcomes)
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
        string learningOutcomes)
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
        return true;
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

    public async Task<bool> SetStatusAsync(int courseId, string ownerId, CourseStatus status)
    {
        var course = await _db.Set<Course>()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == ownerId);
        if (course is null)
        {
            return false;
        }

        course.Status = status;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<int> GetLessonCountAsync(int courseId)
        => _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .SelectMany(m => m.Lessons)
            .CountAsync();

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
        => _db.Set<Course>().AsNoTracking()
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync();

    /// <summary>
    /// Paginated catalog search over published courses. Returns the page and
    /// the total count for the matched set (not the entire page).
    /// </summary>
    public async Task<CourseSearchResult> SearchAsync(
        string? search,
        string? category,
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
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term)
                || c.Description.ToLower().Contains(term)
                || c.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            query = query.Where(c => c.Category == cat);
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
            .ToListAsync();

        return new CourseSearchResult(courses, total);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _db.Set<Course>().AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published && c.Category != string.Empty)
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}
