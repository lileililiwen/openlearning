using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

public class ModuleService
{
    private readonly DbContext _db;

    public ModuleService(DbContext db)
    {
        _db = db;
    }

    public Task<List<Module>> GetForCourseAsync(int courseId)
        => _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .Include(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .ToListAsync();

    public Task<Module?> GetByIdAsync(int id)
        => _db.Set<Module>().AsNoTracking()
            .Include(m => m.Course)
            .Include(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .FirstOrDefaultAsync(m => m.Id == id);

    public Task<List<Lesson>> GetLessonsAsync(int moduleId)
        => _db.Set<Lesson>().AsNoTracking()
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();

    public Task<Course?> GetCourseAsync(int courseId)
        => _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);

    public Task<bool> IsOwnerAsync(int courseId, string userId)
        => _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);

    public async Task<Module?> AddAsync(int courseId, string ownerId, string title)
    {
        if (!await IsOwnerAsync(courseId, ownerId))
        {
            return null;
        }

        var nextOrder = await _db.Set<Module>()
            .Where(m => m.CourseId == courseId)
            .Select(m => (int?)m.OrderIndex)
            .MaxAsync() ?? 0;

        var module = new Module { CourseId = courseId, Title = title, OrderIndex = nextOrder + 1 };
        _db.Set<Module>().Add(module);
        await _db.SaveChangesAsync();
        return module;
    }

    public async Task<bool> UpdateAsync(int moduleId, string ownerId, string title)
    {
        var module = await _db.Set<Module>()
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == moduleId);
        if (module?.Course is null || module.Course.InstructorId != ownerId)
        {
            return false;
        }

        module.Title = title;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int moduleId, string ownerId)
    {
        var module = await _db.Set<Module>()
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == moduleId);
        if (module?.Course is null || module.Course.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<Module>().Remove(module);
        await _db.SaveChangesAsync();
        return true;
    }
}
