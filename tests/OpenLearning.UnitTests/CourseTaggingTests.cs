using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.CourseManagement;

public sealed class CourseTaggingTests
{
    private static readonly string[] _csharpBeginnerTags = { "c#", "Beginner" };
    private static readonly string[] _csharpTags = { "c#" };
    private static readonly string[] _blazorTags = { "blazor" };

    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static CourseService CreateService(ApplicationDbContext db)
    {
        return new CourseService(db, new TagService(db));
    }

    private static async Task<Course> CreateCourseAsync(ApplicationDbContext db, string title, string[] tags)
    {
        if (!await db.Set<ApplicationUser>().AnyAsync(u => u.Id == "instructor-1"))
        {
            db.Set<ApplicationUser>().Add(new ApplicationUser
            {
                Id = "instructor-1",
                UserName = "instructor-1",
                DisplayName = "Instructor",
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(db);
        var course = await service.CreateAsync(
            "instructor-1", title, "Desc", "Programming", null, CourseLevel.Beginner,
            "6 hours", "English", "", "", tags);
        course!.Status = CourseStatus.Published;
        await db.SaveChangesAsync();
        return course;
    }

    [Fact]
    public async Task CreateAsync_attaches_and_auto_creates_tags()
    {
        var db = CreateDb();
        var course = await CreateCourseAsync(db, "C# Basics", _csharpBeginnerTags);

        var loaded = await db.Set<Course>()
            .Include(c => c.Tags).ThenInclude(ct => ct.Tag)
            .SingleAsync(c => c.Id == course.Id);

        Assert.Equal(2, loaded.Tags.Count);
        Assert.Equal(2, await db.Set<Tag>().CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_replaces_tags()
    {
        var db = CreateDb();
        var course = await CreateCourseAsync(db, "C# Basics", _csharpTags);
        var service = CreateService(db);

        var updated = await service.UpdateAsync(
            course.Id, "instructor-1", "C# Basics", "Desc", "Programming", null,
            CourseLevel.Beginner, "6 hours", "English", "", "", tagNames: _blazorTags);

        Assert.True(updated);
        var loaded = await db.Set<Course>()
            .Include(c => c.Tags).ThenInclude(ct => ct.Tag)
            .SingleAsync(c => c.Id == course.Id);
        Assert.Single(loaded.Tags);
        Assert.Equal("blazor", loaded.Tags.Single().Tag.Slug);
    }

    [Fact]
    public async Task SearchAsync_filters_by_tag_slug()
    {
        var db = CreateDb();
        await CreateCourseAsync(db, "C# Basics", _csharpTags);
        await CreateCourseAsync(db, "Blazor Apps", _blazorTags);
        var service = CreateService(db);

        var result = await service.SearchAsync(null, null, "c", CourseSort.Newest, 1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("C# Basics", result.Courses[0].Title);
    }

    [Fact]
    public async Task SearchAsync_returns_empty_for_unknown_tag()
    {
        var db = CreateDb();
        await CreateCourseAsync(db, "C# Basics", _csharpTags);
        var service = CreateService(db);

        var result = await service.SearchAsync(null, null, "missing", CourseSort.Newest, 1, 10);

        Assert.Equal(0, result.TotalCount);
    }
}
