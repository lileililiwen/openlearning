using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using OpenLearning.Progress.Models;

namespace OpenLearning.Progress.Services;

public class ProgressService
{
    private readonly DbContext _db;

    public ProgressService(DbContext db)
    {
        _db = db;
    }

    private Task<EnrollmentEntity?> GetEnrollmentAsync(string studentId, int courseId)
        => _db.Set<EnrollmentEntity>()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

    public async Task<(bool Ok, string? Error)> MarkCompleteAsync(string studentId, int courseId, int lessonId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        if (enrollment is null)
        {
            return (false, "You must be enrolled in this course to track progress.");
        }

        var lessonBelongsToCourse = await _db.Set<Lesson>()
            .AnyAsync(l => l.Id == lessonId && l.Module!.CourseId == courseId);
        if (!lessonBelongsToCourse)
        {
            return (false, "Lesson does not belong to this course.");
        }

        var alreadyComplete = await _db.Set<LessonCompletion>()
            .AnyAsync(lc => lc.EnrollmentId == enrollment.Id && lc.LessonId == lessonId);
        if (!alreadyComplete)
        {
            _db.Set<LessonCompletion>().Add(new LessonCompletion { EnrollmentId = enrollment.Id, LessonId = lessonId });
            await _db.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UnmarkAsync(string studentId, int courseId, int lessonId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        if (enrollment is null)
        {
            return (false, "You must be enrolled in this course to track progress.");
        }

        var completion = await _db.Set<LessonCompletion>()
            .FirstOrDefaultAsync(lc => lc.EnrollmentId == enrollment.Id && lc.LessonId == lessonId);
        if (completion is not null)
        {
            _db.Set<LessonCompletion>().Remove(completion);
            await _db.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<HashSet<int>> GetCompletedLessonIdsAsync(string studentId, int courseId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        if (enrollment is null)
        {
            return new HashSet<int>();
        }

        var ids = await _db.Set<LessonCompletion>().AsNoTracking()
            .Where(lc => lc.EnrollmentId == enrollment.Id)
            .Select(lc => lc.LessonId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task<int> GetProgressPercentAsync(string studentId, int courseId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        if (enrollment is null)
        {
            return 0;
        }

        var totalLessons = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .SelectMany(m => m.Lessons)
            .CountAsync();
        if (totalLessons == 0)
        {
            return 0;
        }

        var completedLessons = await _db.Set<LessonCompletion>()
            .CountAsync(lc => lc.EnrollmentId == enrollment.Id);

        return (int)Math.Round(completedLessons * 100.0 / totalLessons);
    }
}
