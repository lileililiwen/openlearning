using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Progress.Models;
using OpenLearning.Progress.Services;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.StudyTools;

public sealed class StudyToolServiceTests
{
    private static (ApplicationDbContext Db, StudyToolService Service) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new StudyToolService(db, new ProgressService(db)));
    }

    private static async Task<(int CourseId, int LessonId, int EnrollmentId)> SeedCourseLessonAsync(
        ApplicationDbContext db, string studentId = "s1")
    {
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
                    Lessons = new List<Lesson> { new() { Title = "L1" } },
                },
            },
        };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        var lessonId = course.Modules.SelectMany(m => m.Lessons).Single().Id;
        var enrollment = new EnrollmentEntity { StudentId = studentId, CourseId = course.Id };
        db.Set<EnrollmentEntity>().Add(enrollment);
        await db.SaveChangesAsync();
        return (course.Id, lessonId, enrollment.Id);
    }

    [Fact]
    public async Task UpsertNote_creates_then_updates_and_rejects_empty()
    {
        var (db, service) = Create();
        var (_, lessonId, _) = await SeedCourseLessonAsync(db);

        var (created, _) = await service.UpsertNoteAsync("s1", lessonId, "first note");
        var (updated, _) = await service.UpsertNoteAsync("s1", lessonId, "revised note");
        var (empty, emptyError) = await service.UpsertNoteAsync("s1", lessonId, "   ");

        Assert.True(created);
        Assert.True(updated);
        Assert.False(empty);
        Assert.NotNull(emptyError);
        Assert.Single(db.Set<LessonNote>());
        Assert.Equal("revised note", (await service.GetNoteAsync("s1", lessonId))!.Body);
    }

    [Fact]
    public async Task GetNote_returns_null_when_missing_and_isolates_users()
    {
        var (db, service) = Create();
        var (_, lessonId, _) = await SeedCourseLessonAsync(db);
        await service.UpsertNoteAsync("s1", lessonId, "mine");

        Assert.Null(await service.GetNoteAsync("s2", lessonId));
        Assert.Equal("mine", (await service.GetNoteAsync("s1", lessonId))!.Body);
    }

    [Fact]
    public void ToMarkdown_contains_title_and_body()
    {
        var markdown = StudyToolService.ToMarkdown("My Lesson", "line one\nline two");

        Assert.Contains("# My Lesson", markdown);
        Assert.Contains("line one", markdown);
        Assert.EndsWith("\n", markdown);
    }

    [Fact]
    public async Task CheckIn_upserts_same_day_instead_of_duplicating()
    {
        var (db, service) = Create();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var (first, _) = await service.CheckInAsync("s1", "morning");
        var (second, _) = await service.CheckInAsync("s1", "afternoon");

        Assert.True(first);
        Assert.True(second);
        Assert.Single(db.Set<StudyCheckIn>());
        var row = await service.GetCheckInAsync("s1", today);
        Assert.NotNull(row);
        Assert.Equal("afternoon", row.Note);
    }

    [Fact]
    public async Task GetCheckIns_returns_only_the_requested_range()
    {
        var (db, service) = Create();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await service.CheckInAsync("s1", "today");
        db.Set<StudyCheckIn>().Add(new StudyCheckIn { UserId = "s1", Day = today.AddDays(-13) });
        await db.SaveChangesAsync();

        var inRange = await service.GetCheckInsAsync("s1", today.AddDays(-2), today);
        var outOfRange = await service.GetCheckInsAsync("s1", today.AddDays(-15), today.AddDays(-11));

        Assert.Single(inRange);
        Assert.Single(outOfRange);
    }

    [Fact]
    public async Task GetDownloads_returns_only_allowed_files()
    {
        var (db, service) = Create();
        var (_, lessonId, _) = await SeedCourseLessonAsync(db);
        db.Set<LessonDownload>().AddRange(
            new LessonDownload { LessonId = lessonId, FileUrl = "/files/a.pdf", Label = "Courseware", IsAllowed = true },
            new LessonDownload { LessonId = lessonId, FileUrl = "/files/b.pdf", Label = "Answers", IsAllowed = false });
        await db.SaveChangesAsync();

        var downloads = await service.GetDownloadsAsync(lessonId);

        var download = Assert.Single(downloads);
        Assert.Equal("Courseware", download.Label);
    }

    [Fact]
    public async Task GetReport_aggregates_duration_checkins_streak_and_completed()
    {
        var (db, service) = Create();
        var (courseId, lessonId, enrollmentId) = await SeedCourseLessonAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        db.Set<StudySession>().Add(new StudySession
        {
            UserId = "s1",
            CourseId = courseId,
            LessonId = lessonId,
            EnrollmentId = enrollmentId,
            DurationSeconds = 300,
        });
        db.Set<LessonCompletion>().Add(new LessonCompletion { EnrollmentId = enrollmentId, LessonId = lessonId });
        db.Set<StudyCheckIn>().AddRange(
            new StudyCheckIn { UserId = "s1", Day = today },
            new StudyCheckIn { UserId = "s1", Day = today.AddDays(-1) });
        await db.SaveChangesAsync();

        var report = await service.GetReportAsync("s1");

        Assert.Equal(300, report.TotalSeconds);
        Assert.Equal(2, report.CheckInCount);
        Assert.Equal(2, report.CurrentStreakDays);
        Assert.Equal(1, report.CompletedLessons);
    }
}
