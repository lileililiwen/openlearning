using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Enrollment.Services;

/// <summary>
/// One row of the instructor's course roster: enrollment metadata plus
/// the student's progress and last activity timestamp.
/// </summary>
public sealed record RosterEntry(
    int EnrollmentId,
    string StudentId,
    string StudentName,
    string StudentEmail,
    DateTime EnrolledAt,
    int ProgressPercent,
    int CompletedLessons,
    int TotalLessons,
    DateTime? LastAccessedAt);

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

    public Task<int> GetEnrollmentCountAsync(int courseId)
        => _db.Set<EnrollmentEntity>().CountAsync(e => e.CourseId == courseId);

    public Task<int> GetTotalEnrollmentsAsync()
        => _db.Set<EnrollmentEntity>().CountAsync();

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

    /// <summary>
    /// Enrollments (with student) for the given course plus the total lesson
    /// count of the course. Per-student completion/last-access is composed by
    /// the caller from <see cref="OpenLearning.Progress.Services.ProgressService"/>
    /// — this method stays Progress-free to keep the module graph acyclic.
    /// </summary>
    public async Task<(List<EnrollmentEntity> Enrollments, int TotalLessons)> GetEnrollmentsForRosterAsync(int courseId)
    {
        var enrollments = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync();

        var totalLessons = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .SelectMany(m => m.Lessons)
            .CountAsync();

        return (enrollments, totalLessons);
    }

    /// <summary>Enrollment count per course id, for a set of course ids.</summary>
    public async Task<Dictionary<int, int>> GetEnrollmentCountsAsync(IEnumerable<int> courseIds)
    {
        var ids = courseIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var grouped = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => ids.Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToListAsync();
        return grouped.ToDictionary(g => g.CourseId, g => g.Count);
    }

    // ===== Platform analytics (admin reports) =====

    /// <summary>Enrollment count per day in the range.</summary>
    public async Task<List<(DateTime Day, int Count)>> GetEnrollmentsOverTimeAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<EnrollmentEntity> query = _db.Set<EnrollmentEntity>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(e => e.EnrolledAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(e => e.EnrolledAt < end);
        }

        var rows = await query
            .GroupBy(e => e.EnrolledAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(r => r.Day)
            .ToListAsync();
        return rows.Select(r => (r.Day, r.Count)).ToList();
    }

    /// <summary>Enrollment count per course in the range.</summary>
    public sealed record EnrollmentsByCourseRow(int CourseId, string CourseTitle, int Count);

    public async Task<List<EnrollmentsByCourseRow>> GetEnrollmentsByCourseAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<EnrollmentEntity> query = _db.Set<EnrollmentEntity>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(e => e.EnrolledAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(e => e.EnrolledAt < end);
        }

        var enrollments = await query
            .Include(e => e.Course)
            .ToListAsync();

        return enrollments
            .GroupBy(e => new { e.CourseId, CourseTitle = e.Course?.Title ?? string.Empty })
            .Select(g => new EnrollmentsByCourseRow(g.Key.CourseId, g.Key.CourseTitle, g.Count()))
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    /// <summary>Enrollments in the range with course/student, for CSV export.</summary>
    public async Task<List<EnrollmentEntity>> GetEnrollmentsForExportAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<EnrollmentEntity> query = _db.Set<EnrollmentEntity>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(e => e.EnrolledAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(e => e.EnrolledAt < end);
        }

        return await query
            .Include(e => e.Course)
            .Include(e => e.Student)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
    }

    /// <summary>Date-only inputs bind with Kind=Unspecified, which Npgsql rejects for timestamptz.</summary>
    private static DateTime? NormalizeUtc(DateTime? value)
        => value is null
            ? null
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
