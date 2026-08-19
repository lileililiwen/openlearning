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
    public async Task SetStatusAsync_allows_publish_when_instructor_verified_but_routes_under_review()
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
        Assert.Equal(CourseStatus.UnderReview, (await db.Set<Course>().SingleAsync()).Status);
    }

    [Fact]
    public async Task SetStatusAsync_republication_of_published_course_stays_published()
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
            Status = CourseStatus.Published,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (ok, error) = await service.SetStatusAsync(1, "instructor-1", CourseStatus.Published);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(CourseStatus.Published, (await db.Set<Course>().SingleAsync()).Status);
    }

    [Fact]
    public async Task ApproveAsync_publishes_and_RejectAsync_returns_to_draft_with_note()
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
            Status = CourseStatus.UnderReview,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        Assert.True(await service.ApproveAsync(1, "Looks good"));
        Assert.Equal(CourseStatus.Published, (await db.Set<Course>().SingleAsync()).Status);

        var course = await db.Set<Course>().SingleAsync();
        course.Status = CourseStatus.UnderReview;
        await db.SaveChangesAsync();

        Assert.True(await service.RejectAsync(1, "Missing syllabus"));
        var rejected = await db.Set<Course>().SingleAsync();
        Assert.Equal(CourseStatus.Draft, rejected.Status);
        Assert.Equal("Missing syllabus", rejected.ReviewNote);
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
