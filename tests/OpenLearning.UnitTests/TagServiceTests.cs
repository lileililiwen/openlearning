using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.CourseManagement;

public sealed class TagServiceTests
{
    private static readonly string[] _firstTags = { "C#", "ASP.NET Core" };
    private static readonly string[] _secondTags = { "c#", "Blazor" };
    private static readonly string[] _messyTags = { "", "  ", "A", "A", "a" };

    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Theory]
    [InlineData("C#", "c")]
    [InlineData("ASP.NET Core", "asp-net-core")]
    [InlineData("  Beginner  ", "beginner")]
    [InlineData("Web & Mobile", "web-mobile")]
    public void Slugify_normalizes_names(string name, string expected)
    {
        Assert.Equal(expected, TagService.Slugify(name));
    }

    [Fact]
    public async Task EnsureByNamesAsync_creates_unknown_and_reuses_existing()
    {
        var db = CreateDb();
        var service = new TagService(db);

        var first = await service.EnsureByNamesAsync(_firstTags);
        var second = await service.EnsureByNamesAsync(_secondTags);

        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.Equal(3, await db.Set<Tag>().CountAsync());
        Assert.Equal("c", second[0].Slug); // reused, not re-created
        Assert.Equal("blazor", second[1].Slug);
    }

    [Fact]
    public async Task EnsureByNamesAsync_handles_empty_and_duplicates()
    {
        var db = CreateDb();
        var service = new TagService(db);

        var tags = await service.EnsureByNamesAsync(_messyTags);

        Assert.Single(tags);
        Assert.Equal(1, await db.Set<Tag>().CountAsync());
    }
}
