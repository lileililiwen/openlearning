using Microsoft.EntityFrameworkCore;
using OpenLearning.Certificates.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Progress.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Certificates.Services;

/// <summary>
/// Issues and retrieves completion certificates. Issuance is idempotent:
/// calling <see cref="EnsureIssuedAsync"/> repeatedly never duplicates a
/// certificate for the same enrollment.
/// </summary>
public class CertificateService
{
    private const string _alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";

    private readonly DbContext _db;
    private readonly ProgressService _progress;

    public CertificateService(DbContext db, ProgressService progress)
    {
        _db = db;
        _progress = progress;
    }

    /// <summary>
    /// Issues a certificate when the student has reached 100% progress in the
    /// course. Returns the existing certificate if already issued (no dupes),
    /// the freshly created one, or null when progress is below 100% or the
    /// enrollment does not exist.
    /// </summary>
    public async Task<Certificate?> EnsureIssuedAsync(string studentId, int courseId)
    {
        var enrollment = await _db.Set<EnrollmentEntity>()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (enrollment is null)
        {
            return null;
        }

        var existing = await _db.Set<Certificate>()
            .FirstOrDefaultAsync(c => c.EnrollmentId == enrollment.Id);
        if (existing is not null)
        {
            return existing;
        }

        var percent = await _progress.GetProgressPercentAsync(studentId, courseId);
        if (percent < 100)
        {
            return null;
        }

        var certificate = new Certificate
        {
            EnrollmentId = enrollment.Id,
            CourseId = courseId,
            UserId = studentId,
            Code = GenerateCode(),
        };
        _db.Set<Certificate>().Add(certificate);
        await _db.SaveChangesAsync();
        return certificate;
    }

    public Task<Certificate?> GetForEnrollmentAsync(int enrollmentId)
    {
        return _db.Set<Certificate>().AsNoTracking()
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);
    }

    /// <summary>All certificates earned by a student, newest first.</summary>
    public Task<List<Certificate>> GetForUserAsync(string userId)
    {
        return _db.Set<Certificate>().AsNoTracking()
                .Include(c => c.Course)!.ThenInclude(c => c!.Instructor)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync();
    }

    /// <summary>Course ids the student has already earned a certificate for.</summary>
    public async Task<HashSet<int>> GetEarnedCourseIdsAsync(string userId)
    {
        var ids = await _db.Set<Certificate>().AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.CourseId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    /// <summary>Full certificate with student, course, and instructor for the print page.</summary>
    public Task<Certificate?> GetByIdAsync(int id)
    {
        return _db.Set<Certificate>().AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Course)!.ThenInclude(c => c!.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id);
    }

    private static string GenerateCode()
    {
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = _alphabet[Random.Shared.Next(_alphabet.Length)];
        }
        return $"CRT-{new string(chars)}";
    }
}
