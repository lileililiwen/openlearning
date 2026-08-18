using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.CourseManagement;

public sealed class TagAdminTests
{
    private static readonly string[] _csharpTags = { "c#" };
    private static readonly string[] _blazorTags = { "blazor" };
    private static readonly string[] _bothTags = { "c#", "blazor" };

    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<(ApplicationDbContext Db, TagService Service)> SetupAsync()
    {
        var db = CreateDb();
        var service = new TagService(db);
        await service.EnsureByNamesAsync(_bothTags);
        return (db, service);
    }

    [Fact]
    public async Task RenameAsync_updates_name_keeps_slug()
    {
        var (db, service) = await SetupAsync();
        var tag = await db.Set<Tag>().SingleAsync(t => t.Slug == "c");

        var (ok, error) = await service.RenameAsync(tag.Id, "CSharp");

        Assert.True(ok);
        Assert.Null(error);
        var updated = await db.Set<Tag>().SingleAsync(t => t.Slug == "c");
        Assert.Equal("CSharp", updated.Name);
    }

    [Fact]
    public async Task MergeAsync_repoints_joins_and_deletes_source()
    {
        var db = CreateDb();
        var service = new TagService(db);
        var csharp = (await service.EnsureByNamesAsync(_csharpTags))[0];
        var blazor = (await service.EnsureByNamesAsync(_blazorTags))[0];
        db.Set<Course>().AddRange(
            new Course { Id = 1, Title = "A", InstructorId = "i1", Tags = new List<CourseTag>() },
            new Course { Id = 2, Title = "B", InstructorId = "i1", Tags = new List<CourseTag>() });
        await db.SaveChangesAsync();
        db.Set<CourseTag>().AddRange(
            new CourseTag { CourseId = 1, TagId = csharp.Id },
            new CourseTag { CourseId = 2, TagId = blazor.Id });
        await db.SaveChangesAsync();

        var (ok, error) = await service.MergeAsync(csharp.Id, blazor.Id);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Null(await db.Set<Tag>().FindAsync(csharp.Id));
        var joins = await db.Set<CourseTag>().ToListAsync();
        Assert.Equal(2, joins.Count);
        Assert.All(joins, j => Assert.Equal(blazor.Id, j.TagId));
    }

    [Fact]
    public async Task MergeAsync_into_itself_fails()
    {
        var (db, service) = await SetupAsync();
        var tag = await db.Set<Tag>().SingleAsync(t => t.Slug == "c");

        var (ok, error) = await service.MergeAsync(tag.Id, tag.Id);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RetireAsync_hides_from_active_list()
    {
        var (db, service) = await SetupAsync();
        var tag = await db.Set<Tag>().SingleAsync(t => t.Slug == "c");

        var (ok, error) = await service.RetireAsync(tag.Id);

        Assert.True(ok);
        Assert.Null(error);
        Assert.DoesNotContain(await service.GetActiveAsync(), t => t.Id == tag.Id);
        Assert.Contains(await service.GetAllAsync(), t => t.Id == tag.Id);
    }
}
