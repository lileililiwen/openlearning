using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Progress.Services;
using OpenLearning.StudyTools.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.StudyTools.Services;

/// <summary>Aggregate numbers for the student study report.</summary>
public sealed record StudyReport(
    int TotalSeconds,
    int CheckInCount,
    int CurrentStreakDays,
    int CompletedLessons);

/// <summary>
/// Lesson notes, daily check-ins, the study calendar/report, and permitted
/// lesson downloads. Enrollment gating is enforced by the calling pages; the
/// service owns the data operations (like the assignments module).
/// </summary>
public class StudyToolService
{
    private readonly DbContext _db;
    private readonly ProgressService _progress;

    public StudyToolService(DbContext db, ProgressService progress)
    {
        _db = db;
        _progress = progress;
    }

    // ===== Lesson notes =====

    public Task<LessonNote?> GetNoteAsync(string userId, int lessonId)
    {
        return _db.Set<LessonNote>().AsNoTracking()
            .FirstOrDefaultAsync(n => n.UserId == userId && n.LessonId == lessonId);
    }

    public async Task<(bool Ok, string? Error)> UpsertNoteAsync(string userId, int lessonId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (false, "The note cannot be empty.");
        }

        var lessonExists = await _db.Set<Lesson>().AnyAsync(l => l.Id == lessonId);
        if (!lessonExists)
        {
            return (false, "Lesson not found.");
        }

        var note = await _db.Set<LessonNote>()
            .FirstOrDefaultAsync(n => n.UserId == userId && n.LessonId == lessonId);
        if (note is null)
        {
            _db.Set<LessonNote>().Add(new LessonNote
            {
                UserId = userId,
                LessonId = lessonId,
                Body = body.Trim(),
            });
        }
        else
        {
            note.Body = body.Trim();
            note.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Renders a note as a downloadable Markdown document.</summary>
    public static string ToMarkdown(string lessonTitle, string body)
    {
        return $"# {lessonTitle}\n\n{body}\n";
    }

    // ===== Check-ins =====

    /// <summary>Records (or updates) today's check-in. One per UTC day.</summary>
    public async Task<(bool Ok, string? Error)> CheckInAsync(string userId, string? note)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = await _db.Set<StudyCheckIn>()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Day == today);
        if (existing is null)
        {
            _db.Set<StudyCheckIn>().Add(new StudyCheckIn
            {
                UserId = userId,
                Day = today,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            });
        }
        else
        {
            existing.Note = string.IsNullOrWhiteSpace(note) ? existing.Note : note.Trim();
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<StudyCheckIn?> GetCheckInAsync(string userId, DateOnly day)
    {
        return _db.Set<StudyCheckIn>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Day == day);
    }

    public Task<List<StudyCheckIn>> GetCheckInsAsync(string userId, DateOnly from, DateOnly to)
    {
        return _db.Set<StudyCheckIn>().AsNoTracking()
            .Where(c => c.UserId == userId && c.Day >= from && c.Day <= to)
            .OrderBy(c => c.Day)
            .ToListAsync();
    }

    /// <summary>Per-day counted study seconds within [from, to] (from progress tracking).</summary>
    public Task<Dictionary<DateOnly, int>> GetDailyDurationsAsync(string userId, DateOnly from, DateOnly to)
    {
        return _progress.GetDailyDurationsAsync(userId, from, to);
    }

    public async Task<StudyReport> GetReportAsync(string userId)
    {
        // Direct scalar query (no navigation includes) keeps the report cheap
        // and avoids InMemory test-provider Include quirks.
        var courseIds = await _db.Set<EnrollmentEntity>()
            .Where(e => e.StudentId == userId)
            .Select(e => e.CourseId)
            .ToListAsync();

        var totalSeconds = 0;
        var completedLessons = 0;
        foreach (var courseId in courseIds)
        {
            totalSeconds += await _progress.GetCourseDurationAsync(userId, courseId);
            completedLessons += (await _progress.GetCompletedLessonIdsAsync(userId, courseId)).Count;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkIns = await GetCheckInsAsync(userId, today.AddDays(-365), today);

        return new StudyReport(totalSeconds, checkIns.Count, ComputeStreak(checkIns.Select(c => c.Day)), completedLessons);
    }

    private static int ComputeStreak(IEnumerable<DateOnly> days)
    {
        var set = days.ToHashSet();
        var streak = 0;
        var cursor = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!set.Contains(cursor))
        {
            cursor = cursor.AddDays(-1); // a streak that ended yesterday still counts
        }

        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    // ===== Downloads =====

    public Task<List<LessonDownload>> GetDownloadsAsync(int lessonId)
    {
        return _db.Set<LessonDownload>().AsNoTracking()
            .Where(d => d.LessonId == lessonId && d.IsAllowed)
            .OrderBy(d => d.Label)
            .ToListAsync();
    }
}
