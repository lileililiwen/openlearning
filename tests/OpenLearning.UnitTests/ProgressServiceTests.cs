using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Progress.Models;
using OpenLearning.Progress.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Progress;

public sealed class ProgressServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private sealed record Seeded(
        ApplicationDbContext Db,
        int CourseId,
        int Lesson1Id,
        int Lesson2Id,
        int EnrollmentId);

    private static Seeded SeedCourseWithTwoLessons(string studentId = "s1")
    {
        var db = CreateDb();
        var course = new Course
        {
            Title = "C1",
            InstructorId = "i1",
            Status = CourseStatus.Published,
            Modules = new List<Module>
            {
                new()
                {
                    Title = "M1",
                    Lessons = new List<Lesson>
                    {
                        new() { Title = "L1" },
                        new() { Title = "L2" },
                    },
                },
            },
        };
        db.Set<Course>().Add(course);
        db.SaveChanges();
        var lessonIds = course.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).ToList();
        var enrollment = new EnrollmentEntity { StudentId = studentId, CourseId = course.Id };
        db.Set<EnrollmentEntity>().Add(enrollment);
        db.SaveChanges();
        return new Seeded(db, course.Id, lessonIds[0], lessonIds[1], enrollment.Id);
    }

    [Fact]
    public async Task MarkComplete_fails_when_student_is_not_enrolled()
    {
        var seeded = SeedCourseWithTwoLessons();

        var (ok, error) = await new ProgressService(seeded.Db)
            .MarkCompleteAsync("other", seeded.CourseId, seeded.Lesson1Id);

        Assert.False(ok);
        Assert.Contains("enrolled", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkComplete_fails_when_lesson_is_not_in_the_course()
    {
        var seeded = SeedCourseWithTwoLessons();
        var other = CreateDb();
        other.Set<Course>().Add(new Course { Title = "Other", InstructorId = "i", Status = CourseStatus.Published });
        await other.SaveChangesAsync();
        var foreignLessonId = (await other.Set<Course>().SingleAsync()).Id * 1000;

        var (ok, error) = await new ProgressService(seeded.Db)
            .MarkCompleteAsync("s1", seeded.CourseId, foreignLessonId);

        Assert.False(ok);
        Assert.Contains("belong", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkComplete_records_completion_and_is_idempotent()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        var first = await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var second = await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);

        Assert.True(first.Ok);
        Assert.True(second.Ok);
        Assert.Equal(1, await seeded.Db.Set<LessonCompletion>().CountAsync());
    }

    [Fact]
    public async Task Unmark_removes_completion_and_returns_true_for_missing()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);

        var unmarked = await service.UnmarkAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var missing = await service.UnmarkAsync("s1", seeded.CourseId, seeded.Lesson2Id);

        Assert.True(unmarked.Ok);
        Assert.True(missing.Ok);
        Assert.Empty(seeded.Db.Set<LessonCompletion>());
    }

    [Fact]
    public async Task GetProgressPercent_returns_zero_for_not_enrolled_or_empty_course()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        Assert.Equal(0, await service.GetProgressPercentAsync("other", seeded.CourseId));

        var empty = CreateDb();
        empty.Set<Course>().Add(new Course { Title = "Empty", InstructorId = "i", Status = CourseStatus.Published });
        empty.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = 1 });
        await empty.SaveChangesAsync();
        Assert.Equal(0, await new ProgressService(empty).GetProgressPercentAsync("s1", 1));
    }

    [Fact]
    public async Task GetProgressPercent_rounds_partial_and_full_completion()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);

        Assert.Equal(50, await service.GetProgressPercentAsync("s1", seeded.CourseId));

        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson2Id);
        Assert.Equal(100, await service.GetProgressPercentAsync("s1", seeded.CourseId));
    }

    [Fact]
    public async Task RecordAccess_fails_when_not_enrolled_or_foreign_lesson()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        var (notEnrolled, _) = await service.RecordAccessAsync("other", seeded.CourseId, seeded.Lesson1Id);
        var (foreign, _) = await service.RecordAccessAsync("s1", seeded.CourseId, 999_999);

        Assert.False(notEnrolled);
        Assert.False(foreign);
    }

    [Fact]
    public async Task RecordAccess_creates_then_updates_the_access_row()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var row = await seeded.Db.Set<LessonAccess>().SingleAsync();
        var originalStamp = row.LastAccessedAt;

        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson1Id);

        Assert.Single(seeded.Db.Set<LessonAccess>());
        Assert.NotEqual(originalStamp, (await seeded.Db.Set<LessonAccess>().SingleAsync()).LastAccessedAt);
    }

    [Fact]
    public async Task GetContinueLearningItems_returns_most_recent_unfinished_lesson()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson2Id);

        var items = await service.GetContinueLearningItemsAsync("s1");

        var item = Assert.Single(items);
        Assert.Equal(seeded.Lesson2Id, item.LessonId);
        Assert.Equal("C1", item.CourseTitle);
        Assert.Equal("L2", item.LessonTitle);
    }

    [Fact]
    public async Task GetLastAccess_returns_null_then_the_timestamp()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        Assert.Null(await service.GetLastAccessAsync("s1", seeded.CourseId));

        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        Assert.NotNull(await service.GetLastAccessAsync("s1", seeded.CourseId));
    }

    [Fact]
    public async Task GetCourseCompletionRate_returns_null_without_lessons_or_enrollments()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        Assert.Null(await service.GetCourseCompletionRateAsync(999_999));

        var noLessons = CreateDb();
        noLessons.Set<Course>().Add(new Course { Title = "NoLessons", InstructorId = "i", Status = CourseStatus.Published });
        await noLessons.SaveChangesAsync();
        Assert.Null(await new ProgressService(noLessons).GetCourseCompletionRateAsync((await noLessons.Set<Course>().SingleAsync()).Id));
    }

    [Fact]
    public async Task GetCourseCompletionRate_computes_finished_enrollment_percentage()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson2Id);

        Assert.Equal(100, await service.GetCourseCompletionRateAsync(seeded.CourseId));
    }

    [Fact]
    public async Task GetPlatformCompletionRate_returns_null_when_nothing_to_measure()
    {
        var empty = CreateDb();
        Assert.Null(await new ProgressService(empty).GetPlatformCompletionRateAsync());
    }

    [Fact]
    public async Task GetEnrollmentProgressMap_returns_empty_for_empty_input()
    {
        var seeded = SeedCourseWithTwoLessons();

        var (completed, lastAccess) = await new ProgressService(seeded.Db)
            .GetEnrollmentProgressMapAsync(new List<int>());

        Assert.Empty(completed);
        Assert.Empty(lastAccess);
    }

    [Fact]
    public async Task GetEnrollmentProgressMap_aggregates_counts_and_latest_access()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        await service.MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson2Id);

        var (completed, lastAccess) = await service.GetEnrollmentProgressMapAsync(new List<int> { seeded.EnrollmentId });

        Assert.Equal(1, completed[seeded.EnrollmentId]);
        Assert.True(lastAccess.ContainsKey(seeded.EnrollmentId));
    }

    // ===== Study sessions =====

    [Fact]
    public async Task StartSession_fails_when_not_enrolled_or_foreign_lesson()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        var (notEnrolledId, notEnrolledError) = await service.StartSessionAsync("other", seeded.CourseId, seeded.Lesson1Id);
        var (foreignId, foreignError) = await service.StartSessionAsync("s1", seeded.CourseId, 999_999);

        Assert.Null(notEnrolledId);
        Assert.Contains("enrolled", notEnrolledError, StringComparison.OrdinalIgnoreCase);
        Assert.Null(foreignId);
        Assert.Contains("belong", foreignError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartSession_ends_previous_active_session_for_same_lesson()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        var (firstId, _) = await service.StartSessionAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var (secondId, _) = await service.StartSessionAsync("s1", seeded.CourseId, seeded.Lesson1Id);

        var sessions = await seeded.Db.Set<StudySession>().ToListAsync();
        Assert.Equal(2, sessions.Count);
        var first = sessions.Single(s => s.Id == firstId);
        Assert.NotNull(first.EndedAt);
        Assert.Null(sessions.Single(s => s.Id == secondId).EndedAt);
    }

    [Fact]
    public async Task Heartbeat_accumulates_elapsed_time_and_excludes_idle_gaps()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        var (sessionId, _) = await service.StartSessionAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var session = await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId);

        // 30 seconds of activity -> counted.
        session.LastActiveAt = DateTime.UtcNow.AddSeconds(-30);
        await seeded.Db.SaveChangesAsync();
        Assert.True((await service.HeartbeatAsync(sessionId!.Value, "s1")).Ok);

        // 5 minutes away (well beyond the 2x-heartbeat idle gap) -> not counted.
        session = await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId);
        session.LastActiveAt = DateTime.UtcNow.AddSeconds(-300);
        await seeded.Db.SaveChangesAsync();
        Assert.True((await service.HeartbeatAsync(sessionId.Value, "s1")).Ok);

        var duration = (await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId)).DurationSeconds;
        Assert.InRange(duration, 20, 40);
    }

    [Fact]
    public async Task EndSession_accumulates_trailing_time_and_sets_ended_at()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        var (sessionId, _) = await service.StartSessionAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var session = await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId);
        session.LastActiveAt = DateTime.UtcNow.AddSeconds(-30);
        await seeded.Db.SaveChangesAsync();

        Assert.True((await service.EndSessionAsync(sessionId!.Value, "s1")).Ok);

        var ended = await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId);
        Assert.NotNull(ended.EndedAt);
        Assert.InRange(ended.DurationSeconds, 20, 40);
    }

    [Fact]
    public async Task Daily_cap_limits_accumulation()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        var (sessionId, _) = await service.StartSessionAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        var session = await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId);
        session.DurationSeconds = ProgressService.DailyCapSeconds - 20;
        session.LastActiveAt = DateTime.UtcNow.AddSeconds(-30);
        await seeded.Db.SaveChangesAsync();

        await service.HeartbeatAsync(sessionId!.Value, "s1");

        var duration = (await seeded.Db.Set<StudySession>().SingleAsync(s => s.Id == sessionId)).DurationSeconds;
        Assert.Equal(ProgressService.DailyCapSeconds, duration);
    }

    [Fact]
    public async Task Duration_queries_aggregate_by_lesson_course_and_enrollment()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        seeded.Db.Set<StudySession>().AddRange(
            new StudySession { UserId = "s1", CourseId = seeded.CourseId, LessonId = seeded.Lesson1Id, EnrollmentId = seeded.EnrollmentId, DurationSeconds = 60 },
            new StudySession { UserId = "s1", CourseId = seeded.CourseId, LessonId = seeded.Lesson2Id, EnrollmentId = seeded.EnrollmentId, DurationSeconds = 120 },
            new StudySession { UserId = "s1", CourseId = seeded.CourseId, LessonId = seeded.Lesson1Id, EnrollmentId = seeded.EnrollmentId, DurationSeconds = 90 });
        await seeded.Db.SaveChangesAsync();

        Assert.Equal(150, await service.GetLessonDurationAsync("s1", seeded.Lesson1Id));
        Assert.Equal(270, await service.GetCourseDurationAsync("s1", seeded.CourseId));
        Assert.Equal(270, (await service.GetDurationByEnrollmentAsync(new List<int> { seeded.EnrollmentId }))[seeded.EnrollmentId]);
        Assert.Equal(0, await service.GetLessonDurationAsync("s1", seeded.Lesson2Id + 1));
    }

    [Fact]
    public async Task GetDailyDurations_groups_by_utc_start_day()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        seeded.Db.Set<StudySession>().AddRange(
            new StudySession { UserId = "s1", CourseId = seeded.CourseId, LessonId = seeded.Lesson1Id, StartedAt = DateTime.UtcNow, DurationSeconds = 60 },
            new StudySession { UserId = "s1", CourseId = seeded.CourseId, LessonId = seeded.Lesson2Id, StartedAt = DateTime.UtcNow, DurationSeconds = 120 },
            new StudySession { UserId = "s1", CourseId = seeded.CourseId, LessonId = seeded.Lesson1Id, StartedAt = DateTime.UtcNow.AddDays(-1), DurationSeconds = 90 });
        await seeded.Db.SaveChangesAsync();

        var daily = await service.GetDailyDurationsAsync("s1", today.AddDays(-7), today);

        Assert.Equal(180, daily[today]);
        Assert.Equal(90, daily[today.AddDays(-1)]);
        Assert.False(daily.ContainsKey(today.AddDays(1)));
    }

    [Fact]
    public async Task SavePosition_and_GetPosition_require_access_row_and_clamp_negative()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = new ProgressService(seeded.Db);

        // No LessonAccess row yet -> position stays 0 and save is ignored.
        Assert.Equal(0, await service.GetPositionAsync("s1", seeded.CourseId, seeded.Lesson1Id));
        await service.SavePositionAsync("s1", seeded.CourseId, seeded.Lesson1Id, 42);
        Assert.Equal(0, await service.GetPositionAsync("s1", seeded.CourseId, seeded.Lesson1Id));

        // After RecordAccessAsync the position persists.
        await service.RecordAccessAsync("s1", seeded.CourseId, seeded.Lesson1Id);
        await service.SavePositionAsync("s1", seeded.CourseId, seeded.Lesson1Id, 42);
        Assert.Equal(42, await service.GetPositionAsync("s1", seeded.CourseId, seeded.Lesson1Id));

        // Negative seconds clamp to 0; non-enrolled users always read 0.
        await service.SavePositionAsync("s1", seeded.CourseId, seeded.Lesson1Id, -5);
        Assert.Equal(0, await service.GetPositionAsync("s1", seeded.CourseId, seeded.Lesson1Id));
        Assert.Equal(0, await service.GetPositionAsync("other", seeded.CourseId, seeded.Lesson1Id));
    }
}
