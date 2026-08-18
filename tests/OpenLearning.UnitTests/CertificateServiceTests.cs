using Microsoft.EntityFrameworkCore;
using OpenLearning.Certificates.Models;
using OpenLearning.Certificates.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Progress.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Certificates;

public sealed class CertificateServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private sealed record Seeded(ApplicationDbContext Db, int CourseId, int Lesson1Id, int Lesson2Id, int EnrollmentId);

    private static Seeded SeedCourseWithTwoLessons()
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
        var enrollment = new EnrollmentEntity { StudentId = "s1", CourseId = course.Id };
        db.Set<EnrollmentEntity>().Add(enrollment);
        db.SaveChanges();
        return new Seeded(db, course.Id, lessonIds[0], lessonIds[1], enrollment.Id);
    }

    private static CertificateService CreateService(ApplicationDbContext db)
    {
        return new CertificateService(db, new ProgressService(db));
    }

    private static async Task CompleteAllLessonsAsync(ApplicationDbContext db, int courseId, int lesson1Id, int lesson2Id)
    {
        var progress = new ProgressService(db);
        await progress.MarkCompleteAsync("s1", courseId, lesson1Id);
        await progress.MarkCompleteAsync("s1", courseId, lesson2Id);
    }

    [Fact]
    public async Task EnsureIssued_returns_null_when_not_enrolled()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = CreateService(seeded.Db);

        Assert.Null(await service.EnsureIssuedAsync("other", seeded.CourseId));
    }

    [Fact]
    public async Task EnsureIssued_returns_null_below_100_percent()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = CreateService(seeded.Db);
        await new ProgressService(seeded.Db).MarkCompleteAsync("s1", seeded.CourseId, seeded.Lesson1Id);

        Assert.Null(await service.EnsureIssuedAsync("s1", seeded.CourseId));
    }

    [Fact]
    public async Task EnsureIssued_issues_at_100_percent_and_is_idempotent()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = CreateService(seeded.Db);
        await CompleteAllLessonsAsync(seeded.Db, seeded.CourseId, seeded.Lesson1Id, seeded.Lesson2Id);

        var first = await service.EnsureIssuedAsync("s1", seeded.CourseId);
        var second = await service.EnsureIssuedAsync("s1", seeded.CourseId);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.StartsWith("CRT-", first.Code);
        Assert.Single(seeded.Db.Set<Certificate>());
    }

    [Fact]
    public async Task EnsureIssued_returns_the_existing_certificate()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = CreateService(seeded.Db);
        await CompleteAllLessonsAsync(seeded.Db, seeded.CourseId, seeded.Lesson1Id, seeded.Lesson2Id);
        var issued = await service.EnsureIssuedAsync("s1", seeded.CourseId);

        var fetched = await service.GetForEnrollmentAsync(seeded.EnrollmentId);

        Assert.NotNull(fetched);
        Assert.NotNull(issued);
        Assert.Equal(issued.Id, fetched.Id);
    }

    [Fact]
    public async Task GetEarnedCourseIds_returns_empty_then_the_course()
    {
        var seeded = SeedCourseWithTwoLessons();
        var service = CreateService(seeded.Db);

        Assert.Empty(await service.GetEarnedCourseIdsAsync("s1"));

        await CompleteAllLessonsAsync(seeded.Db, seeded.CourseId, seeded.Lesson1Id, seeded.Lesson2Id);
        await service.EnsureIssuedAsync("s1", seeded.CourseId);

        Assert.Contains(seeded.CourseId, await service.GetEarnedCourseIdsAsync("s1"));
    }
}
