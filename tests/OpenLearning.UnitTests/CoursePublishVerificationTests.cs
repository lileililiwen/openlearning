using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.CourseManagement;

public sealed class CoursePublishVerificationTests
{
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

    [Fact]
    public async Task SetStatusAsync_blocks_publish_when_instructor_unverified()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "instructor-1",
            UserName = "instructor-1",
            IdentityStatus = IdentityStatus.Unverified,
        });
        db.Set<Course>().Add(new Course
        {
            Id = 1,
            Title = "Course",
            InstructorId = "instructor-1",
            Status = CourseStatus.Draft,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (ok, error) = await service.SetStatusAsync(1, "instructor-1", CourseStatus.Published);

        Assert.False(ok);
        Assert.Contains("verified", error, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CourseStatus.Draft, (await db.Set<Course>().SingleAsync()).Status);
    }

    [Fact]
    public async Task SetStatusAsync_allows_publish_when_instructor_verified()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "instructor-1",
            UserName = "instructor-1",
            IdentityStatus = IdentityStatus.Verified,
            VerifiedAt = DateTime.UtcNow,
        });
        db.Set<Course>().Add(new Course
        {
            Id = 1,
            Title = "Course",
            InstructorId = "instructor-1",
            Status = CourseStatus.Draft,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (ok, error) = await service.SetStatusAsync(1, "instructor-1", CourseStatus.Published);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(CourseStatus.Published, (await db.Set<Course>().SingleAsync()).Status);
    }

    [Fact]
    public async Task SetStatusAsync_allows_unpublish_without_verification()
    {
        var db = CreateDb();
        db.Set<ApplicationUser>().Add(new ApplicationUser
        {
            Id = "instructor-1",
            UserName = "instructor-1",
            IdentityStatus = IdentityStatus.Unverified,
        });
        db.Set<Course>().Add(new Course
        {
            Id = 1,
            Title = "Course",
            InstructorId = "instructor-1",
            Status = CourseStatus.Published,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (ok, error) = await service.SetStatusAsync(1, "instructor-1", CourseStatus.Draft);

        Assert.True(ok);
        Assert.Null(error);
    }
}
