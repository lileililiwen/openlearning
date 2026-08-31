using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests;

public sealed class LessonNavigatorServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static (ApplicationDbContext Db, int CourseId, int M1L1, int M1L2, int M2L1, int M2L2) SeedTwoModuleCourse()
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
                    Title = "Module A",
                    OrderIndex = 0,
                    Lessons = new List<Lesson>
                    {
                        new() { Title = "A1", OrderIndex = 0 },
                        new() { Title = "A2", OrderIndex = 1 },
                    },
                },
                new()
                {
                    Title = "Module B",
                    OrderIndex = 1,
                    Lessons = new List<Lesson>
                    {
                        new() { Title = "B1", OrderIndex = 0 },
                        new() { Title = "B2", OrderIndex = 1 },
                    },
                },
            },
        };
        db.Set<Course>().Add(course);
        db.SaveChanges();

        var ids = course.Modules
            .OrderBy(m => m.OrderIndex)
            .SelectMany(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .Select(l => l.Id)
            .ToList();

        return (db, course.Id, ids[0], ids[1], ids[2], ids[3]);
    }

    [Fact]
    public async Task GetOrderedLessonsAsync_returns_modules_in_order()
    {
        var (db, courseId, _, _, _, _) = SeedTwoModuleCourse();
        var service = new LessonNavigatorService(db);

        var outline = await service.GetOrderedLessonsAsync(courseId);

        Assert.Equal(2, outline.Count);
        Assert.Equal("Module A", outline[0].ModuleTitle);
        Assert.Equal(2, outline[0].Lessons.Count);
        Assert.Equal("A1", outline[0].Lessons[0].Title);
        Assert.Equal("A2", outline[0].Lessons[1].Title);
        Assert.Equal("Module B", outline[1].ModuleTitle);
        Assert.Equal("B1", outline[1].Lessons[0].Title);
        Assert.Equal("B2", outline[1].Lessons[1].Title);
    }

    [Fact]
    public async Task GetOrderedLessonsAsync_marks_completed_lessons()
    {
        var (db, courseId, m1l1, _, _, _) = SeedTwoModuleCourse();
        var service = new LessonNavigatorService(db);
        var completed = new HashSet<int> { m1l1 };

        var outline = await service.GetOrderedLessonsAsync(courseId, completed);

        Assert.True(outline[0].Lessons[0].IsCompleted);
        Assert.False(outline[0].Lessons[1].IsCompleted);
    }

    [Fact]
    public async Task GetNavigatorAsync_returns_next_across_modules()
    {
        var (db, courseId, m1l1, m1l2, m2l1, _) = SeedTwoModuleCourse();
        var service = new LessonNavigatorService(db);

        var nav = await service.GetNavigatorAsync(courseId, m1l2);

        Assert.Equal(m1l1, nav.PrevLessonId);
        Assert.Equal(m2l1, nav.NextLessonId);
    }

    [Fact]
    public async Task GetNavigatorAsync_returns_null_at_first_lesson()
    {
        var (db, courseId, m1l1, _, _, _) = SeedTwoModuleCourse();
        var service = new LessonNavigatorService(db);

        var nav = await service.GetNavigatorAsync(courseId, m1l1);

        Assert.Null(nav.PrevLessonId);
        Assert.NotNull(nav.NextLessonId);
    }

    [Fact]
    public async Task GetNavigatorAsync_returns_null_at_last_lesson()
    {
        var (db, courseId, _, _, _, m2l2) = SeedTwoModuleCourse();
        var service = new LessonNavigatorService(db);

        var nav = await service.GetNavigatorAsync(courseId, m2l2);

        Assert.NotNull(nav.PrevLessonId);
        Assert.Null(nav.NextLessonId);
    }

    [Fact]
    public async Task GetNavigatorAsync_returns_nulls_for_unknown_lesson()
    {
        var (db, courseId, _, _, _, _) = SeedTwoModuleCourse();
        var service = new LessonNavigatorService(db);

        var nav = await service.GetNavigatorAsync(courseId, 99999);

        Assert.Null(nav.PrevLessonId);
        Assert.Null(nav.NextLessonId);
    }

    [Fact]
    public async Task GetOrderedLessonsAsync_returns_empty_for_course_with_no_modules()
    {
        var db = CreateDb();
        db.Set<Course>().Add(new Course { Title = "Empty", InstructorId = "i", Status = CourseStatus.Published });
        await db.SaveChangesAsync();
        var courseId = (await db.Set<Course>().SingleAsync()).Id;
        var service = new LessonNavigatorService(db);

        var outline = await service.GetOrderedLessonsAsync(courseId);

        Assert.Empty(outline);
    }
}
