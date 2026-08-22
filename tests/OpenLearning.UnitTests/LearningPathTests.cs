using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.LearningPaths.Models;
using OpenLearning.LearningPaths.Services;
using OpenLearning.Progress.Models;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using ModuleEntity = OpenLearning.CourseManagement.Models.Module;

namespace OpenLearning.UnitTests;

public sealed class LearningPathTests
{
    [Fact]
    public async Task Publishing_rejects_prerequisite_cycle()
    {
        await using TestDb db = CreateDb();
        List<Course> courses = SeedCourses(db, 2);
        await db.SaveChangesAsync();
        LearningPath path = PathWithDraft(courses, prerequisiteCycle: true);
        db.Add(path);
        await db.SaveChangesAsync();
        var service = new LearningPathService(db);

        var result = await service.PublishAsync(path.Id, "owner", false);

        Assert.False(result.Ok);
        Assert.Contains("cycle", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task New_publication_does_not_rewrite_existing_assignment()
    {
        await using TestDb db = CreateDb();
        List<Course> courses = SeedCourses(db, 1);
        await db.SaveChangesAsync();
        LearningPath path = PathWithDraft(courses);
        db.Add(path);
        await db.SaveChangesAsync();
        var service = new LearningPathService(db);
        Assert.True((await service.PublishAsync(path.Id, "owner", false)).Ok);
        PathEnrollment first = (await service.EnrollAsync(path.Id, "student-1")).Enrollment!;

        Assert.True((await service.PublishAsync(path.Id, "owner", false)).Ok);
        PathEnrollment second = (await service.EnrollAsync(path.Id, "student-2")).Enrollment!;

        Assert.NotEqual(first.LearningPathVersionId, second.LearningPathVersionId);
        Assert.Equal(1, await db.Set<LearningPathVersion>().Where(x => x.Id == first.LearningPathVersionId).Select(x => x.VersionNumber).SingleAsync());
    }

    [Fact]
    public async Task Progress_blocks_prerequisite_and_requires_elective_threshold()
    {
        await using TestDb db = CreateDb();
        List<Course> courses = SeedCourses(db, 3);
        await db.SaveChangesAsync();
        LearningPath path = PathWithDraft(courses, electiveMinimum: 1, successorPrerequisite: true);
        db.Add(path);
        await db.SaveChangesAsync();
        var service = new LearningPathService(db);
        Assert.True((await service.PublishAsync(path.Id, "owner", false)).Ok);
        PathEnrollment assigned = (await service.EnrollAsync(path.Id, "student")).Enrollment!;

        PathProgress? initial = await service.GetProgressAsync(assigned.Id, "student");
        Assert.Equal(PathCourseState.Blocked, initial!.Courses.Single(x => x.CourseId == courses[1].Id).State);
        Assert.False(initial.IsComplete);

        await CompleteCourse(db, "student", courses[0]);
        await CompleteCourse(db, "student", courses[1]);
        PathProgress? requiredOnly = await service.GetProgressAsync(assigned.Id, "student");
        Assert.False(requiredOnly!.IsComplete);
        await CompleteCourse(db, "student", courses[2]);
        PathProgress? complete = await service.GetProgressAsync(assigned.Id, "student");
        Assert.True(complete!.IsComplete);
        Assert.NotNull(complete.CompletedAt);
    }

    private static TestDb CreateDb()
    {
        return new(new DbContextOptionsBuilder<TestDb>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private static List<Course> SeedCourses(TestDb db, int count)
    {
        var courses = Enumerable.Range(1, count).Select(i => new Course
        {
            Title = $"Course {i}",
            Status = CourseStatus.Published,
            InstructorId = "owner",
            Modules = [ new() { Title = "Module", OrderIndex = 1,
              Lessons = [new() { Title = "Lesson", OrderIndex = 1 }] } ]
        }).ToList();
        db.AddRange(courses);
        return courses;
    }

    private static LearningPath PathWithDraft(IReadOnlyList<Course> courses, bool prerequisiteCycle = false,
        int electiveMinimum = 0, bool successorPrerequisite = false)
    {
        var items = courses.Select((course, index) => new LearningPathCourse
        {
            CourseId = course.Id,
            Position = index + 1,
            IsRequired = index < 2,
            PrerequisiteCourseId = successorPrerequisite && index == 1 ? courses[0].Id : null
        }).ToList();
        if (prerequisiteCycle)
        { items[0].PrerequisiteCourseId = courses[1].Id; items[1].PrerequisiteCourseId = courses[0].Id; }
        return new LearningPath
        {
            Title = "Path",
            OwnerId = "owner",
            Versions = [new() { VersionNumber = 1, Stages = [new() { Title = "Stage", Position = 1, MinimumElectives = electiveMinimum, Courses = items }] }]
        };
    }

    private static async Task CompleteCourse(TestDb db, string studentId, Course course)
    {
        var enrollment = new EnrollmentEntity { StudentId = studentId, CourseId = course.Id };
        db.Add(enrollment);
        await db.SaveChangesAsync();
        db.Add(new LessonCompletion { EnrollmentId = enrollment.Id, LessonId = course.Modules.Single().Lessons.Single().Id });
        await db.SaveChangesAsync();
    }

    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<CourseTag>();
            modelBuilder.Entity<Course>();
            modelBuilder.Entity<ModuleEntity>();
            modelBuilder.Entity<Lesson>();
            modelBuilder.Entity<EnrollmentEntity>();
            modelBuilder.Entity<LessonCompletion>();
            modelBuilder.Entity<LearningPath>();
            modelBuilder.Entity<LearningPathVersion>();
            modelBuilder.Entity<LearningPathStage>();
            modelBuilder.Entity<LearningPathCourse>();
            modelBuilder.Entity<PathEnrollment>();
        }
    }
}
