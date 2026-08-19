using Microsoft.EntityFrameworkCore;
using OpenLearning.Classes.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Classes.Services;

/// <summary>Owner-gated CRUD for class groups plus enrollment tagging with capacity checks.</summary>
public class ClassGroupService
{
    private readonly DbContext _db;

    public ClassGroupService(DbContext db)
    {
        _db = db;
    }

    public Task<List<ClassGroup>> GetForCourseAsync(int courseId)
    {
        return _db.Set<ClassGroup>().AsNoTracking()
                .Where(c => c.CourseId == courseId)
                .OrderByDescending(c => c.EndsAt)
                .ToListAsync();
    }

    public Task<ClassGroup?> GetByIdAsync(int id)
    {
        return _db.Set<ClassGroup>().AsNoTracking()
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<bool> IsOwnerAsync(int classGroupId, string userId)
    {
        return _db.Set<ClassGroup>().AsNoTracking()
                .AnyAsync(c => c.Id == classGroupId && c.Course!.InstructorId == userId);
    }

    public Task<bool> IsCourseOwnerAsync(int courseId, string userId)
    {
        return _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    public async Task<(ClassGroup? Class, string? Error)> CreateAsync(
        int courseId, string ownerId, string name, DateTime startsAt, DateTime endsAt, int? capacity)
    {
        if (!await IsCourseOwnerAsync(courseId, ownerId))
        {
            return (null, "You do not own this course.");
        }

        if (endsAt <= startsAt)
        {
            return (null, "End time must be after start time.");
        }

        var classGroup = new ClassGroup
        {
            CourseId = courseId,
            Name = name,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Capacity = capacity,
        };
        _db.Set<ClassGroup>().Add(classGroup);
        await _db.SaveChangesAsync();
        return (classGroup, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(int classGroupId, string ownerId, string name, DateTime startsAt, DateTime endsAt, int? capacity)
    {
        var classGroup = await _db.Set<ClassGroup>()
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == classGroupId);
        if (classGroup?.Course is null || classGroup.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        if (endsAt <= startsAt)
        {
            return (false, "End time must be after start time.");
        }

        classGroup.Name = name;
        classGroup.StartsAt = startsAt;
        classGroup.EndsAt = endsAt;
        classGroup.Capacity = capacity;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int classGroupId, string ownerId)
    {
        var classGroup = await _db.Set<ClassGroup>()
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == classGroupId);
        if (classGroup?.Course is null || classGroup.Course.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<ClassGroup>().Remove(classGroup);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Owner may manually close (or reopen) a class; Open is derived from time.</summary>
    public async Task<bool> SetStatusAsync(int classGroupId, string ownerId, ClassGroupStatus status)
    {
        if (status == ClassGroupStatus.Open)
        {
            return false; // Open is computed, never stored.
        }

        var classGroup = await _db.Set<ClassGroup>()
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == classGroupId);
        if (classGroup?.Course is null || classGroup.Course.InstructorId != ownerId)
        {
            return false;
        }

        classGroup.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Attaches an existing enrollment to a class, enforcing the capacity cap.</summary>
    public async Task<(bool Ok, string? Error)> EnrollIntoClassAsync(int classGroupId, int enrollmentId, string ownerId)
    {
        var classGroup = await _db.Set<ClassGroup>()
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == classGroupId);
        if (classGroup?.Course is null || classGroup.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        var enrollment = await _db.Set<EnrollmentEntity>()
            .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.CourseId == classGroup.CourseId);
        if (enrollment is null)
        {
            return (false, "Enrollment not found in this course.");
        }

        if (classGroup.Capacity is { } capacity)
        {
            var count = await _db.Set<EnrollmentEntity>()
                .CountAsync(e => e.ClassGroupId == classGroupId);
            if (count >= capacity)
            {
                return (false, "This class has reached its capacity.");
            }
        }

        enrollment.ClassGroupId = classGroupId;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
