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
    {
        return _db.Set<EnrollmentEntity>().AsNoTracking()
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Course)!.ThenInclude(c => c!.Instructor)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();
    }

    public Task<bool> IsEnrolledAsync(string studentId, int courseId)
    {
        // Revoked enrollments do not grant access; the learner must re-enroll.
        return _db.Set<EnrollmentEntity>().AnyAsync(e =>
            e.StudentId == studentId && e.CourseId == courseId && e.RevokedAt == null);
    }

    public Task<int> GetEnrollmentCountAsync(int courseId)
    {
        return _db.Set<EnrollmentEntity>().CountAsync(e => e.CourseId == courseId);
    }

    public Task<int> GetTotalEnrollmentsAsync()
    {
        return _db.Set<EnrollmentEntity>().CountAsync();
    }

    /// <summary>
    /// Enrolls a student. <paramref name="membershipExpiresAt"/> is the active
    /// membership's expiry (if the enrollment uses the membership benefit); the
    /// resulting access expiry is min(membership expiry, course default).
    /// </summary>
    public async Task<(bool Ok, string? Error)> EnrollAsync(string studentId, int courseId, DateTime? membershipExpiresAt = null)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null || course.Status != CourseStatus.Published)
        {
            return (false, "Course not found or not published.");
        }

        var alreadyEnrolled = await _db.Set<EnrollmentEntity>()
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.RevokedAt == null);
        if (alreadyEnrolled)
        {
            return (false, "You are already enrolled in this course.");
        }

        var now = DateTime.UtcNow;
        var courseExpiry = course.DefaultAccessDays is int days && days > 0
            ? (DateTime?)now.AddDays(days)
            : null;
        DateTime? accessExpiresAt;
        if (membershipExpiresAt is DateTime membershipExpiry)
        {
            if (courseExpiry is DateTime courseDeadline && courseDeadline < membershipExpiry)
            {
                accessExpiresAt = courseDeadline;
            }
            else
            {
                accessExpiresAt = membershipExpiry;
            }
        }
        else
        {
            accessExpiresAt = courseExpiry;
        }

        _db.Set<EnrollmentEntity>().Add(new EnrollmentEntity
        {
            StudentId = studentId,
            CourseId = courseId,
            AccessExpiresAt = accessExpiresAt,
        });
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

    // ===== Access period =====

    /// <summary>
    /// True when the student's active (non-revoked) enrollment for the course
    /// is past its access expiry — used to block write actions during the
    /// grace period.
    /// </summary>
    public async Task<bool> IsAccessExpiredAsync(string studentId, int courseId)
    {
        var expired = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.CourseId == courseId && e.RevokedAt == null)
            .Select(e => e.AccessExpiresAt)
            .FirstOrDefaultAsync();
        return expired is DateTime deadline && DateTime.UtcNow > deadline;
    }

    /// <summary>Sets (or clears) the access expiry on an enrollment. Owner or admin/finance only.</summary>
    public async Task<(bool Ok, string? Error)> SetExpiryAsync(
        int enrollmentId, DateTime? expiresAt, string actorId, bool isAdminOrFinance)
    {
        var enrollment = await _db.Set<EnrollmentEntity>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
        if (enrollment is null)
        {
            return (false, "Enrollment not found.");
        }

        if (enrollment.RevokedAt is not null)
        {
            return (false, "This enrollment was revoked.");
        }

        if (!isAdminOrFinance && enrollment.Course?.InstructorId != actorId)
        {
            return (false, "Only the course instructor or an admin can change access period.");
        }

        if (expiresAt is DateTime value && value.Kind != DateTimeKind.Utc)
        {
            expiresAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        enrollment.AccessExpiresAt = expiresAt;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Revokes an enrollment (expiry job, refund, or admin action). Idempotent for already-revoked rows.</summary>
    public async Task<(bool Ok, string? Error)> RevokeAsync(int enrollmentId, string reason, string actorId, bool isAdminOrFinance)
    {
        var enrollment = await _db.Set<EnrollmentEntity>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
        if (enrollment is null)
        {
            return (false, "Enrollment not found.");
        }

        if (!isAdminOrFinance && enrollment.Course?.InstructorId != actorId)
        {
            return (false, "Only the course instructor or an admin can revoke access.");
        }

        if (enrollment.RevokedAt is not null)
        {
            return (false, "This enrollment was already revoked.");
        }

        enrollment.RevokedAt = DateTime.UtcNow;
        enrollment.RevokedReason = string.IsNullOrWhiteSpace(reason) ? "admin" : reason.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Active enrollments whose access expired more than <paramref name="graceDays"/> ago — the revocation candidates.</summary>
    public async Task<List<EnrollmentEntity>> ListExpiredPastGraceAsync(int graceDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-graceDays);
        return await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.RevokedAt == null && e.AccessExpiresAt != null && e.AccessExpiresAt < cutoff)
            .ToListAsync();
    }

    /// <summary>Active enrollments expiring within the next <paramref name="days"/> days — the notify-soon candidates.</summary>
    public async Task<List<EnrollmentEntity>> ListExpiringWithinAsync(int days)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(days);
        return await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.RevokedAt == null && e.AccessExpiresAt != null
                && e.AccessExpiresAt > now && e.AccessExpiresAt <= horizon)
            .ToListAsync();
    }

    /// <summary>Admin/finance enrollment list with optional course/student filters.</summary>
    public async Task<List<EnrollmentEntity>> GetAdminEnrollmentsAsync(int? courseId, string? search)
    {
        IQueryable<EnrollmentEntity> query = _db.Set<EnrollmentEntity>().AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student);

        if (courseId is int id)
        {
            query = query.Where(e => e.CourseId == id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Student != null && (
                e.Student.DisplayName.Contains(term)
                || (e.Student.Email != null && e.Student.Email.Contains(term))));
        }

        return await query.OrderByDescending(e => e.EnrolledAt).Take(300).ToListAsync();
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
    {
        return value is null
                ? null
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }
}
