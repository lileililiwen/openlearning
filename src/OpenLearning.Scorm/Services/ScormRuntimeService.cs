using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Scorm.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Scorm.Services;

public record ScormRuntimeState(
    string LessonLocation,
    string SuspendData,
    string LessonStatus,
    string ScoreRaw,
    string SessionTime);

public class ScormRuntimeService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;

    public ScormRuntimeService(DbContext db, EnrollmentService enrollments, ProgressService progress)
    {
        _db = db;
        _enrollments = enrollments;
        _progress = progress;
    }

    public async Task<(int? EnrollmentId, string? Error)> GetEnrollmentForPackageAsync(string studentId, int packageId)
    {
        var package = await _db.Set<ScormPackage>().AsNoTracking()
            .Include(p => p.Lesson).ThenInclude(l => l!.Module)
            .FirstOrDefaultAsync(p => p.Id == packageId);
        if (package?.Lesson?.Module is null)
        {
            return (null, "Package not found.");
        }

        var courseId = package.Lesson.Module.CourseId;
        if (!await _enrollments.IsEnrolledAsync(studentId, courseId))
        {
            return (null, "You are not enrolled in this course.");
        }

        var enrollment = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        return (enrollment?.Id, null);
    }

    public async Task<List<ScormRecord>> GetRecordsForEnrollmentAsync(int enrollmentId)
    {
        return await _db.Set<ScormRecord>().AsNoTracking()
            .Include(r => r.ScormPackage)
            .Where(r => r.EnrollmentId == enrollmentId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
    }

    public async Task<ScormRuntimeState> GetStateAsync(int enrollmentId, int packageId)
    {
        var record = await _db.Set<ScormRecord>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.ScormPackageId == packageId);

        return new ScormRuntimeState(
            record?.LessonLocation ?? string.Empty,
            record?.SuspendData ?? string.Empty,
            record?.LessonStatus ?? "not attempted",
            record?.ScoreRaw ?? string.Empty,
            record?.SessionTime ?? string.Empty);
    }

    public async Task CommitAsync(int enrollmentId, int packageId, ScormRuntimeState state)
    {
        var record = await _db.Set<ScormRecord>()
            .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.ScormPackageId == packageId);
        if (record is null)
        {
            record = new ScormRecord { EnrollmentId = enrollmentId, ScormPackageId = packageId };
            _db.Set<ScormRecord>().Add(record);
        }

        record.LessonLocation = state.LessonLocation;
        record.SuspendData = state.SuspendData;
        record.LessonStatus = state.LessonStatus;
        record.ScoreRaw = state.ScoreRaw;
        record.SessionTime = state.SessionTime;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (state.LessonStatus is "completed" or "passed")
        {
            await MarkLessonCompleteAsync(enrollmentId, packageId);
        }
    }

    private async Task MarkLessonCompleteAsync(int enrollmentId, int packageId)
    {
        var package = await _db.Set<ScormPackage>().AsNoTracking()
            .Include(p => p.Lesson).ThenInclude(l => l!.Module)
            .FirstOrDefaultAsync(p => p.Id == packageId);
        if (package?.Lesson?.Module is null)
        {
            return;
        }

        var enrollment = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
        if (enrollment is null)
        {
            return;
        }

        await _progress.MarkCompleteAsync(enrollment.StudentId, package.Lesson.Module.CourseId, package.LessonId);
    }
}
