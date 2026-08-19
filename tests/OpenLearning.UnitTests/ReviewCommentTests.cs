using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using OpenLearning.Ratings.Models;
using OpenLearning.Ratings.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Ratings;

public sealed class ReviewCommentTests
{
    private static async Task<(ApplicationDbContext Db, int ReviewId)> SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id });
        await db.SaveChangesAsync();
        var review = new Review { CourseId = course.Id, UserId = "s1", Rating = 5, Comment = "Great" };
        db.Set<Review>().Add(review);
        await db.SaveChangesAsync();
        return (db, review.Id);
    }

    [Fact]
    public async Task Comment_duplicate_guard_and_delete()
    {
        var (db, reviewId) = await SeedAsync();
        var service = new ReviewService(db, new EnrollmentService(db));

        Assert.True((await service.AddCommentAsync(reviewId, "s1", "Agreed")).Ok);
        var (dupOk, dupError) = await service.AddCommentAsync(reviewId, "s1", "Agreed");
        Assert.False(dupOk);
        Assert.NotNull(dupError);

        var comment = await db.Set<ReviewComment>().SingleAsync();
        Assert.True(await service.DeleteCommentAsync(comment.Id));
        Assert.Empty(await db.Set<ReviewComment>().ToListAsync());
    }

    [Fact]
    public async Task Non_enrolled_cannot_comment()
    {
        var (db, reviewId) = await SeedAsync();
        var service = new ReviewService(db, new EnrollmentService(db));

        var (ok, error) = await service.AddCommentAsync(reviewId, "outsider", "Hi");
        Assert.False(ok);
        Assert.NotNull(error);
    }
}
