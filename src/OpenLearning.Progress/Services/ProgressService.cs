using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using OpenLearning.Progress.Models;

namespace OpenLearning.Progress.Services;

/// <summary>The most recently accessed unfinished lesson for one enrollment.</summary>
public sealed record ContinueLearningItem(
    int CourseId,
    string CourseTitle,
    int LessonId,
    string LessonTitle,
    DateTime LastAccessedAt);

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

    /// <summary>
    /// Records that an enrolled Student opened a lesson, making it the resume
    /// point for "continue learning". One row per (enrollment, lesson); the
    /// timestamp is overwritten on every open.
    /// </summary>
    public async Task<(bool Ok, string? Error)> RecordAccessAsync(string studentId, int courseId, int lessonId)
    {
        var enrollment = await GetEnrollmentAsync(studentId, courseId);
        if (enrollment is null)
        {
            return (false, "You must be enrolled in this course to track access.");
        }

        var lessonBelongsToCourse = await _db.Set<Lesson>()
            .AnyAsync(l => l.Id == lessonId && l.Module!.CourseId == courseId);
        if (!lessonBelongsToCourse)
        {
            return (false, "Lesson does not belong to this course.");
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
        return (true, null);
    }

    /// <summary>
    /// For each course the Student is enrolled in, the most recently accessed
    /// lesson that has not been completed — ordered most-recently-touched first.
    /// </summary>
    public async Task<List<ContinueLearningItem>> GetContinueLearningItemsAsync(string studentId)
    {
        var accesses = await _db.Set<LessonAccess>().AsNoTracking()
            .Where(la => la.Enrollment!.StudentId == studentId)
            .Select(la => new
            {
                la.EnrollmentId,
                CourseId = la.Enrollment!.CourseId,
                CourseTitle = la.Enrollment!.Course!.Title,
                la.LessonId,
                LessonTitle = la.Lesson!.Title,
                la.LastAccessedAt,
            })
            .ToListAsync();
        if (accesses.Count == 0)
        {
            return new List<ContinueLearningItem>();
        }

        var completed = await _db.Set<LessonCompletion>().AsNoTracking()
            .Where(lc => lc.Enrollment!.StudentId == studentId)
            .Select(lc => new { lc.EnrollmentId, lc.LessonId })
            .ToListAsync();
        var completedSet = completed.Select(c => (c.EnrollmentId, c.LessonId)).ToHashSet();

        return accesses
            .Where(a => !completedSet.Contains((a.EnrollmentId, a.LessonId)))
            .GroupBy(a => a.CourseId)
            .Select(g => g.OrderByDescending(a => a.LastAccessedAt).First())
            .OrderByDescending(a => a.LastAccessedAt)
            .Select(a => new ContinueLearningItem(
                a.CourseId, a.CourseTitle, a.LessonId, a.LessonTitle, a.LastAccessedAt))
            .ToList();
    }

    /// <summary>
    /// Percentage of enrolled Students in a course who completed every lesson;
    /// null when the course has no lessons or no enrollments.
    /// </summary>
    public async Task<int?> GetCourseCompletionRateAsync(int courseId)
    {
        var totalLessons = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .SelectMany(m => m.Lessons)
            .CountAsync();
        if (totalLessons == 0)
        {
            return null;
        }

        var enrollmentIds = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => e.Id)
            .ToListAsync();
        if (enrollmentIds.Count == 0)
        {
            return null;
        }

        var completedCounts = await _db.Set<LessonCompletion>().AsNoTracking()
            .Where(lc => enrollmentIds.Contains(lc.EnrollmentId))
            .GroupBy(lc => lc.EnrollmentId)
            .Select(g => g.Count())
            .ToListAsync();

        var finished = completedCounts.Count(c => c >= totalLessons);
        return (int)Math.Round(finished * 100.0 / enrollmentIds.Count);
    }

    /// <summary>
    /// Percentage of enrollments in published courses whose students completed
    /// every lesson of the course; null when there is nothing to measure.
    /// </summary>
    public async Task<int?> GetPlatformCompletionRateAsync()
    {
        var published = await _db.Set<Course>().AsNoTracking()
            .Where(c => c.Status == CourseStatus.Published)
            .Select(c => new { c.Id, LessonCount = c.Modules.SelectMany(m => m.Lessons).Count() })
            .ToListAsync();
        var withLessons = published.Where(c => c.LessonCount > 0).ToList();
        if (withLessons.Count == 0)
        {
            return null;
        }

        var courseIds = withLessons.Select(c => c.Id).ToList();
        var enrollments = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .Select(e => new { e.Id, e.CourseId })
            .ToListAsync();
        if (enrollments.Count == 0)
        {
            return null;
        }

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var completedCounts = await _db.Set<LessonCompletion>().AsNoTracking()
            .Where(lc => enrollmentIds.Contains(lc.EnrollmentId))
            .GroupBy(lc => lc.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, Completed = g.Count() })
            .ToListAsync();

        var courseByEnrollment = enrollments.ToDictionary(e => e.Id, e => e.CourseId);
        var lessonCountByCourse = withLessons.ToDictionary(c => c.Id, c => c.LessonCount);

        var finished = completedCounts.Count(c =>
            courseByEnrollment.TryGetValue(c.EnrollmentId, out var courseId)
            && lessonCountByCourse.TryGetValue(courseId, out var total)
            && c.Completed >= total);

        return (int)Math.Round(finished * 100.0 / enrollments.Count);
    }
}
