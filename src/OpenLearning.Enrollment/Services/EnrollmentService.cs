using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Enrollment.Services;

public class EnrollmentService
{
    private readonly DbContext _db;

    public EnrollmentService(DbContext db)
    {
        _db = db;
    }

    public Task<List<EnrollmentEntity>> GetStudentEnrollmentsAsync(string studentId)
        => _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)!.ThenInclude(c => c!.Instructor)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

    public Task<bool> IsEnrolledAsync(string studentId, int courseId)
        => _db.Set<EnrollmentEntity>().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);

    public async Task<(bool Ok, string? Error)> EnrollAsync(string studentId, int courseId)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null || course.Status != CourseStatus.Published)
        {
            return (false, "Course not found or not published.");
        }

        var alreadyEnrolled = await _db.Set<EnrollmentEntity>()
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (alreadyEnrolled)
        {
            return (false, "You are already enrolled in this course.");
        }

        _db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = studentId, CourseId = courseId });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> WithdrawAsync(string studentId, int courseId)
    {
        var enrollment = await _db.Set<EnrollmentEntity>()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (enrollment is null)
        {
            return false;
        }

        _db.Set<EnrollmentEntity>().Remove(enrollment);
        await _db.SaveChangesAsync();
        return true;
    }
}
