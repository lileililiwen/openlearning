using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Progress.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Progress.Services;

/// <summary>
/// Resolves the lesson a learner should resume at for a single course and records
/// the most recently viewed lesson. Backed by the existing <see cref="LessonAccess"/>
/// store (one row per enrollment+lesson), so no dedicated resume column is needed.
/// </summary>
public interface IResumeService
{
    /// <summary>
    /// Records that an enrolled learner opened <paramref name="lessonId"/>, making it
    /// the resume point. No-op when the learner is not enrolled or the lesson does not
    /// belong to the course (e.g. an owner preview or an unauthenticated visitor).
    /// </summary>
    Task RecordViewAsync(string userId, int courseId, int lessonId);

    /// <summary>
    /// Returns the lesson a learner should resume at for <paramref name="courseId"/>:
    /// the most recently viewed lesson that still exists and belongs to the course,
    /// otherwise the first lesson of the course, otherwise <c>null</c> when the learner
    /// is not enrolled or the course has no lessons. Stale targets (deleted/unpublished)
    /// fall back to the first lesson.
    /// </summary>
    Task<int?> GetResumeTargetAsync(string userId, int courseId);
}

public sealed class ResumeService : IResumeService
{
    private readonly DbContext _db;

    public ResumeService(DbContext db)
    {
        _db = db;
    }

    public async Task RecordViewAsync(string userId, int courseId, int lessonId)
    {
        var enrollment = await GetEnrollmentAsync(userId, courseId);
        if (enrollment is null)
        {
            return;
        }

        var belongsToCourse = await _db.Set<Lesson>()
            .AnyAsync(l => l.Id == lessonId && l.Module!.CourseId == courseId);
        if (!belongsToCourse)
        {
            return;
        }

        var access = await _db.Set<LessonAccess>()
            .FirstOrDefaultAsync(la => la.EnrollmentId == enrollment.Id && la.LessonId == lessonId);
        if (access is null)
        {
            _db.Set<LessonAccess>().Add(new LessonAccess
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
            });
        }
        else
        {
            access.LastAccessedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<int?> GetResumeTargetAsync(string userId, int courseId)
    {
        var enrollment = await GetEnrollmentAsync(userId, courseId);
        if (enrollment is null)
        {
            return null;
        }

        var lastLessonId = await _db.Set<LessonAccess>().AsNoTracking()
            .Where(la => la.EnrollmentId == enrollment.Id)
            .OrderByDescending(la => la.LastAccessedAt)
            .Select(la => la.LessonId)
            .FirstOrDefaultAsync();
        if (lastLessonId != 0)
        {
            var stillExists = await _db.Set<Lesson>().AsNoTracking()
                .AnyAsync(l => l.Id == lastLessonId && l.Module!.CourseId == courseId);
            if (stillExists)
            {
                return lastLessonId;
            }
        }

        return await GetFirstLessonIdAsync(courseId);
    }

    private static Task<EnrollmentEntity?> GetEnrollmentAsync(DbContext db, string userId, int courseId)
    {
        return db.Set<EnrollmentEntity>()
            .FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == courseId && e.RevokedAt == null);
    }

    private Task<EnrollmentEntity?> GetEnrollmentAsync(string userId, int courseId)
    {
        return GetEnrollmentAsync(_db, userId, courseId);
    }

    private async Task<int?> GetFirstLessonIdAsync(int courseId)
    {
        return await _db.Set<Lesson>().AsNoTracking()
            .Where(l => l.Module!.CourseId == courseId)
            .OrderBy(l => l.Module!.OrderIndex)
            .ThenBy(l => l.OrderIndex)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
    }
}
