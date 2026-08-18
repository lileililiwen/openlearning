using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

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

    public async Task<Course?> CreateAsync(string instructorId, string title, string description, string category)
    {
        var course = new Course
        {
            Title = title,
            Description = description,
            Category = category,
            InstructorId = instructorId,
        };

        _db.Set<Course>().Add(course);
        await _db.SaveChangesAsync();
        return course;
    }

    public async Task<bool> UpdateAsync(int courseId, string ownerId, string title, string description, string category)
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
}
