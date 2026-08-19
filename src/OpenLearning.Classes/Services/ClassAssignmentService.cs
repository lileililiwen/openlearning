using Microsoft.EntityFrameworkCore;
using OpenLearning.Classes.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Classes.Services;

/// <summary>Assigns TAs / instructors / observers to class groups (unique per class, user, role).</summary>
public class ClassAssignmentService
{
    private readonly DbContext _db;

    public ClassAssignmentService(DbContext db)
    {
        _db = db;
    }

    public Task<List<ClassAssignment>> GetForClassAsync(int classGroupId)
    {
        return _db.Set<ClassAssignment>().AsNoTracking()
                .Where(a => a.ClassGroupId == classGroupId)
                .OrderBy(a => a.AssignedAt)
                .ToListAsync();
    }

    public async Task<(bool Ok, string? Error)> AssignAsync(int classGroupId, string ownerId, string userId, ClassAssignmentRole role)
    {
        var classGroup = await _db.Set<ClassGroup>()
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == classGroupId);
        if (classGroup?.Course is null || classGroup.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        var duplicate = await _db.Set<ClassAssignment>()
            .AnyAsync(a => a.ClassGroupId == classGroupId && a.UserId == userId && a.Role == role);
        if (duplicate)
        {
            return (false, "That user is already assigned to this class in the same role.");
        }

        _db.Set<ClassAssignment>().Add(new ClassAssignment
        {
            ClassGroupId = classGroupId,
            UserId = userId,
            Role = role,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Idempotent revoke of an assignment.</summary>
    public async Task<(bool Ok, string? Error)> RevokeAsync(int assignmentId, string ownerId)
    {
        var assignment = await _db.Set<ClassAssignment>()
            .Include(a => a.ClassGroup)!.ThenInclude(c => c!.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment?.ClassGroup?.Course is null || assignment.ClassGroup.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        _db.Set<ClassAssignment>().Remove(assignment);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<int> GetEnrollmentCountAsync(int classGroupId)
    {
        return _db.Set<OpenLearning.Enrollment.Models.Enrollment>()
            .CountAsync(e => e.ClassGroupId == classGroupId);
    }
}
