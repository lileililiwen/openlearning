using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Progress.Models;
using OpenLearning.Progress.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests;

public sealed class ResumeServiceTests
{
    private static ApplicationDbContext NewDb()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private sealed record Seed(
        ApplicationDbContext Db, int CourseId, int Lesson1Id, int Lesson2Id, int EnrollmentId);

    private static Seed SeedCourseWithEnrollment(string studentId = "s1")
    {
        var db = NewDb();
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
        return new Seed(db, course.Id, lessonIds[0], lessonIds[1], enrollment.Id);
    }

    [Fact]
    public async Task RecordViewAsync_Then_GetResumeTargetAsync_ReturnsRecordedLesson()
    {
        var seed = SeedCourseWithEnrollment();
        var service = new ResumeService(seed.Db);

        await service.RecordViewAsync("s1", seed.CourseId, seed.Lesson2Id);

        var target = await service.GetResumeTargetAsync("s1", seed.CourseId);
        Assert.Equal(seed.Lesson2Id, target);
    }

    [Fact]
    public async Task GetResumeTargetAsync_WithoutProgress_ReturnsFirstLesson()
    {
        var seed = SeedCourseWithEnrollment();
        var service = new ResumeService(seed.Db);

        var target = await service.GetResumeTargetAsync("s1", seed.CourseId);

        Assert.Equal(seed.Lesson1Id, target);
    }

    [Fact]
    public async Task GetResumeTargetAsync_StaleTarget_FallsBackToFirstLesson()
    {
        var seed = SeedCourseWithEnrollment();
        var service = new ResumeService(seed.Db);

        // Point the resume target at a deleted lesson (id 0 -> non-existent in this course).
        await service.RecordViewAsync("s1", seed.CourseId, 999_999);
        seed.Db.Set<LessonAccess>().Add(new LessonAccess
        {
            EnrollmentId = seed.EnrollmentId,
            LessonId = 999_999,
        });
        await seed.Db.SaveChangesAsync();

        var target = await service.GetResumeTargetAsync("s1", seed.CourseId);

        Assert.Equal(seed.Lesson1Id, target);
    }

    [Fact]
    public async Task GetResumeTargetAsync_NotEnrolled_ReturnsNull()
    {
        var seed = SeedCourseWithEnrollment();
        var service = new ResumeService(seed.Db);

        var target = await service.GetResumeTargetAsync("not-enrolled", seed.CourseId);

        Assert.Null(target);
    }

    [Fact]
    public async Task RecordViewAsync_NotEnrolled_DoesNotCreateAccessRow()
    {
        var seed = SeedCourseWithEnrollment();
        var service = new ResumeService(seed.Db);

        await service.RecordViewAsync("not-enrolled", seed.CourseId, seed.Lesson1Id);

        var rows = await seed.Db.Set<LessonAccess>().AsNoTracking().CountAsync();
        Assert.Equal(0, rows);
    }

    [Fact]
    public async Task RecordViewAsync_ForeignLesson_DoesNotCreateAccessRow()
    {
        var seed = SeedCourseWithEnrollment();
        var service = new ResumeService(seed.Db);

        await service.RecordViewAsync("s1", seed.CourseId, 999_999);

        var rows = await seed.Db.Set<LessonAccess>().AsNoTracking().CountAsync();
        Assert.Equal(0, rows);
    }
}
