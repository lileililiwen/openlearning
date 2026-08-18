using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.CourseManagement;

public sealed class CategoryServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Fact]
    public async Task CreateAsync_adds_and_orders_categories()
    {
        var db = CreateDb();
        var service = new CategoryService(db);

        await service.CreateAsync("Programming");
        await service.CreateAsync("Design");

        var categories = await service.GetActiveAsync();
        Assert.Equal(2, categories.Count);
        Assert.Equal("Programming", categories[0].Name);
        Assert.Equal("design", categories[1].Slug);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_slug()
    {
        var db = CreateDb();
        var service = new CategoryService(db);
        await service.CreateAsync("Programming");

        var (ok, error) = await service.CreateAsync("programming");

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RenameAsync_cascades_to_courses()
    {
        var db = CreateDb();
        var service = new CategoryService(db);
        await service.CreateAsync("Programming");
        db.Set<Course>().Add(new Course
        {
            Id = 1,
            Title = "C#",
            Category = "Programming",
            InstructorId = "instructor-1",
        });
        await db.SaveChangesAsync();

        var category = (await service.GetActiveAsync())[0];
        var (ok, error) = await service.RenameAsync(category.Id, "Software Development");

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("Software Development", (await db.Set<Course>().SingleAsync()).Category);
    }

    [Fact]
    public async Task SetActiveAsync_hides_category_from_active_list()
    {
        var db = CreateDb();
        var service = new CategoryService(db);
        await service.CreateAsync("Programming");
        var category = (await service.GetActiveAsync())[0];

        await service.SetActiveAsync(category.Id, false);

        Assert.Empty(await service.GetActiveAsync());
        Assert.Single(await service.GetAllAsync());
    }
}
