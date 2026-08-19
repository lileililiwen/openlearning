using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Progress.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

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
    /// <summary>Maximum counted study time per user per day (4 hours).</summary>
    public const int DailyCapSeconds = 4 * 60 * 60;

    /// <summary>Client heartbeat interval; gaps above twice this count as idle.</summary>
    public const int HeartbeatIntervalSeconds = 60;

    public const int MaxIdleGapSeconds = 2 * HeartbeatIntervalSeconds;

    private readonly DbContext _db;

    public ProgressService(DbContext db)
    {
        _db = db;
    }

    private Task<EnrollmentEntity?> GetEnrollmentAsync(string studentId, int courseId)
    {
        return _db.Set<EnrollmentEntity>()
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
    }

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
                CourseTitle = la.Enrollment.Course!.Title,
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

    /// <summary>
    /// Most recent LessonAccess timestamp for the given (student, course), or
    /// null if the student has not opened any lesson yet.
    /// </summary>
    public async Task<DateTime?> GetLastAccessAsync(string studentId, int courseId)
    {
        var access = await _db.Set<LessonAccess>().AsNoTracking()
            .Where(la => la.Enrollment!.StudentId == studentId && la.Enrollment.CourseId == courseId)
            .OrderByDescending(la => la.LastAccessedAt)
            .FirstOrDefaultAsync();
        return access?.LastAccessedAt;
    }

    /// <summary>
    /// Per-enrollment completion counts and last-access timestamps in two
    /// round-trips. Used by the teacher-roster page to avoid N+1 across a
    /// course's students.
    /// </summary>
    public async Task<(
        Dictionary<int, int> CompletedByEnrollment,
        Dictionary<int, DateTime> LastAccessByEnrollment)>
        GetEnrollmentProgressMapAsync(List<int> enrollmentIds)
    {
        if (enrollmentIds.Count == 0)
        {
            return (new Dictionary<int, int>(), new Dictionary<int, DateTime>());
        }

        var completedCounts = await _db.Set<LessonCompletion>().AsNoTracking()
            .Where(lc => enrollmentIds.Contains(lc.EnrollmentId))
            .GroupBy(lc => lc.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, Completed = g.Count() })
            .ToListAsync();
        var lastAccess = await _db.Set<LessonAccess>().AsNoTracking()
            .Where(la => enrollmentIds.Contains(la.EnrollmentId))
            .GroupBy(la => la.EnrollmentId)
            .Select(g => new { EnrollmentId = g.Key, LastAccessedAt = g.Max(x => x.LastAccessedAt) })
            .ToListAsync();

        return (
            completedCounts.ToDictionary(c => c.EnrollmentId, c => c.Completed),
            lastAccess.ToDictionary(a => a.EnrollmentId, a => a.LastAccessedAt));
    }

    // ===== Study sessions =====

    /// <summary>
    /// Starts a study session for a lesson, ending any still-active session for
    /// the same (user, lesson) first (multi-tab duplicate prevention). Returns
    /// the new session id.
    /// </summary>
    public async Task<(int? SessionId, string? Error)> StartSessionAsync(
        string userId, int courseId, int lessonId)
    {
        var lessonBelongsToCourse = await _db.Set<Lesson>()
            .AnyAsync(l => l.Id == lessonId && l.Module!.CourseId == courseId);
        if (!lessonBelongsToCourse)
        {
            return (null, "Lesson does not belong to this course.");
        }

        var now = DateTime.UtcNow;

        // Close any session still active for the same (user, lesson).
        var active = await _db.Set<StudySession>()
            .Where(s => s.UserId == userId && s.CourseId == courseId && s.LessonId == lessonId && s.EndedAt == null)
            .ToListAsync();
        foreach (var activeSession in active)
        {
            await AccumulateAsync(activeSession, (int)(now - activeSession.LastActiveAt).TotalSeconds);
            activeSession.EndedAt = now;
        }

        var enrollment = await GetEnrollmentAsync(userId, courseId);
        if (enrollment is null)
        {
            return (null, "You must be enrolled in this course to track study time.");
        }

        var session = new StudySession
        {
            UserId = userId,
            CourseId = courseId,
            LessonId = lessonId,
            EnrollmentId = enrollment?.Id,
            StartedAt = now,
            LastActiveAt = now,
        };
        _db.Set<StudySession>().Add(session);
        await _db.SaveChangesAsync();
        return (session.Id, null);
    }

    /// <summary>
    /// Accumulates elapsed time since the last heartbeat. Gaps of two heartbeat
    /// intervals or more are treated as idle and do not count. The counted time
    /// is capped per user per day.
    /// </summary>
    public async Task<(bool Ok, string? Error)> HeartbeatAsync(int sessionId, string userId)
    {
        var session = await _db.Set<StudySession>()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session is null)
        {
            return (false, "Session not found.");
        }

        if (session.EndedAt is not null)
        {
            return (false, "Session already ended.");
        }

        var elapsed = (int)(DateTime.UtcNow - session.LastActiveAt).TotalSeconds;
        if (elapsed >= MaxIdleGapSeconds)
        {
            elapsed = 0; // idle gap excluded
        }

        await AccumulateAsync(session, elapsed);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Finalizes a session, accumulating any trailing time.</summary>
    public async Task<(bool Ok, string? Error)> EndSessionAsync(int sessionId, string userId)
    {
        var session = await _db.Set<StudySession>()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session is null)
        {
            return (false, "Session not found.");
        }

        if (session.EndedAt is not null)
        {
            return (true, null);
        }

        var now = DateTime.UtcNow;
        var elapsed = (int)(now - session.LastActiveAt).TotalSeconds;
        if (elapsed >= MaxIdleGapSeconds)
        {
            elapsed = 0;
        }

        await AccumulateAsync(session, elapsed);
        session.EndedAt = now;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task AccumulateAsync(StudySession session, int elapsed)
    {
        var now = DateTime.UtcNow;
        if (elapsed > 0)
        {
            // Cap applies per user per UTC day, attributed to the session start day.
            var dayStart = session.StartedAt.Date;
            var dayTotal = await _db.Set<StudySession>()
                .Where(s => s.UserId == session.UserId && s.StartedAt >= dayStart && s.StartedAt < dayStart.AddDays(1))
                .SumAsync(s => s.DurationSeconds);
            var remaining = Math.Max(0, DailyCapSeconds - dayTotal);
            session.DurationSeconds += Math.Min(elapsed, remaining);
        }

        session.LastActiveAt = now;
    }

    /// <summary>Total counted seconds for one lesson.</summary>
    public Task<int> GetLessonDurationAsync(string userId, int lessonId)
    {
        return _db.Set<StudySession>().AsNoTracking()
            .Where(s => s.UserId == userId && s.LessonId == lessonId)
            .SumAsync(s => s.DurationSeconds);
    }

    /// <summary>Total counted seconds for one course.</summary>
    public Task<int> GetCourseDurationAsync(string userId, int courseId)
    {
        return _db.Set<StudySession>().AsNoTracking()
            .Where(s => s.UserId == userId && s.CourseId == courseId)
            .SumAsync(s => s.DurationSeconds);
    }

    /// <summary>
    /// Total counted seconds per UTC day within [from, to], attributed to each
    /// session's start day.
    /// </summary>
    public async Task<Dictionary<DateOnly, int>> GetDailyDurationsAsync(
        string userId, DateOnly from, DateOnly to)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rows = await _db.Set<StudySession>().AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAt >= fromUtc && s.StartedAt < toExclusive)
            .Select(s => new { s.StartedAt, s.DurationSeconds })
            .ToListAsync();
        return rows
            .GroupBy(r => DateOnly.FromDateTime(r.StartedAt.Date))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DurationSeconds));
    }

    /// <summary>Total counted seconds per enrollment (used by the teacher roster).</summary>
    public async Task<Dictionary<int, int>> GetDurationByEnrollmentAsync(List<int> enrollmentIds)
    {
        if (enrollmentIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var rows = await _db.Set<StudySession>().AsNoTracking()
            .Where(s => s.EnrollmentId != null && enrollmentIds.Contains(s.EnrollmentId.Value))
            .Select(s => new { s.EnrollmentId, s.DurationSeconds })
            .ToListAsync();
        return rows
            .GroupBy(r => r.EnrollmentId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DurationSeconds));
    }
}
