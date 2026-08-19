using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Moderation.Models;
using OpenLearning.Moderation.Services;
using OpenLearning.Ratings.Models;
using Xunit;

namespace OpenLearning.UnitTests.Moderation;

public sealed class ContentReviewServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<(ApplicationDbContext Db, ContentReviewService Service, Review Review)> SeedReviewAsync()
    {
        var db = CreateDb();
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        db.Set<ApplicationUser>().AddRange(
            new ApplicationUser { Id = "author-1", UserName = "author-1" },
            new ApplicationUser { Id = "reporter-1", UserName = "reporter-1" });
        await db.SaveChangesAsync();

        var review = new Review { CourseId = course.Id, UserId = "author-1", Rating = 5, Comment = "Great course!" };
        db.Set<Review>().Add(review);
        await db.SaveChangesAsync();

        return (db, new ContentReviewService(db), review);
    }

    [Fact]
    public async Task Report_rejects_empty_reason()
    {
        var (db, service, review) = await SeedReviewAsync();

        var (ok, error) = await service.ReportAsync("reporter-1", ReportedContentType.Review, review.Id, "   ");

        Assert.False(ok);
        Assert.Contains("reason", error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(db.Set<ContentReport>());
    }

    [Fact]
    public async Task Report_rejects_self_report()
    {
        var (_, service, review) = await SeedReviewAsync();

        var (ok, error) = await service.ReportAsync("author-1", ReportedContentType.Review, review.Id, "spam");

        Assert.False(ok);
        Assert.Contains("own content", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Report_rejects_missing_content()
    {
        var (_, service, _) = await SeedReviewAsync();

        var (ok, error) = await service.ReportAsync("reporter-1", ReportedContentType.Review, 999_999, "spam");

        Assert.False(ok);
        Assert.Contains("no longer exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Report_creates_open_report_and_duplicate_is_rejected()
    {
        var (db, service, review) = await SeedReviewAsync();

        var first = await service.ReportAsync("reporter-1", ReportedContentType.Review, review.Id, "spam");
        var second = await service.ReportAsync("reporter-1", ReportedContentType.Review, review.Id, "spam");

        Assert.True(first.Ok);
        Assert.False(second.Ok);
        Assert.Contains("already reported", second.Error, StringComparison.OrdinalIgnoreCase);
        var report = Assert.Single(db.Set<ContentReport>());
        Assert.Equal(ReportResolution.Pending, report.Resolution);
        Assert.Equal("reporter-1", report.ReportedById);
    }

    [Fact]
    public async Task Resolve_remove_hides_the_review()
    {
        var (db, service, review) = await SeedReviewAsync();
        await service.ReportAsync("reporter-1", ReportedContentType.Review, review.Id, "spam");
        var report = await db.Set<ContentReport>().SingleAsync();

        var (ok, error) = await service.ResolveAsync(report.Id, remove: true, resolverId: "admin-1");

        Assert.True(ok);
        Assert.Null(error);
        Assert.True((await db.Set<Review>().SingleAsync()).IsHidden);
        Assert.Equal(ReportResolution.Removed, (await db.Set<ContentReport>().SingleAsync()).Resolution);
        Assert.True(await service.IsContentHiddenAsync(ReportedContentType.Review, review.Id));
    }

    [Fact]
    public async Task Resolve_dismiss_keeps_content_visible()
    {
        var (db, service, review) = await SeedReviewAsync();
        await service.ReportAsync("reporter-1", ReportedContentType.Review, review.Id, "spam");
        var report = await db.Set<ContentReport>().SingleAsync();

        var (ok, error) = await service.ResolveAsync(report.Id, remove: false, resolverId: "admin-1");

        Assert.True(ok);
        Assert.Null(error);
        Assert.False((await db.Set<Review>().SingleAsync()).IsHidden);
        Assert.Equal(ReportResolution.Dismissed, (await db.Set<ContentReport>().SingleAsync()).Resolution);
    }

    [Fact]
    public async Task Preview_returns_author_and_snippet()
    {
        var (_, service, review) = await SeedReviewAsync();

        var preview = await service.GetPreviewAsync(ReportedContentType.Review, review.Id);

        Assert.NotNull(preview);
        Assert.Equal("author-1", preview.AuthorId);
        Assert.Contains("Great course", preview.Snippet);
        Assert.Equal(1, preview.CourseId);
    }

    [Fact]
    public async Task Preview_returns_null_for_missing_content()
    {
        var (_, service, _) = await SeedReviewAsync();

        var preview = await service.GetPreviewAsync(ReportedContentType.Review, 999_999);

        Assert.Null(preview);
    }
}
