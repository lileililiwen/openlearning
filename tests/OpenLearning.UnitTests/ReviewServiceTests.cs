using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using OpenLearning.Ratings.Models;
using OpenLearning.Ratings.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Ratings;

public sealed class ReviewServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static (ReviewService Service, ApplicationDbContext Db, int CourseId) SeedCourseAndEnrollment()
    {
        var db = CreateDb();
        var course = new Course { Title = "C1", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = 1 });
        db.SaveChanges();
        var service = new ReviewService(db, new EnrollmentService(db));
        return (service, db, course.Id);
    }

    [Fact]
    public async Task Submit_rejects_rating_out_of_range()
    {
        var (service, _, courseId) = SeedCourseAndEnrollment();

        var (ok, error) = await service.SubmitAsync("s1", courseId, 6, "comment");

        Assert.False(ok);
        Assert.Contains("between 1 and 5", error);
    }

    [Fact]
    public async Task Submit_rejects_missing_course()
    {
        var (service, _, _) = SeedCourseAndEnrollment();

        var (ok, error) = await service.SubmitAsync("s1", 999_999, 5, "comment");

        Assert.False(ok);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Submit_rejects_student_who_is_not_enrolled()
    {
        var (service, _, courseId) = SeedCourseAndEnrollment();

        var (ok, error) = await service.SubmitAsync("other", courseId, 5, "comment");

        Assert.False(ok);
        Assert.Contains("enrolled", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Submit_rejects_comment_over_2000_characters()
    {
        var (service, _, courseId) = SeedCourseAndEnrollment();

        var (ok, error) = await service.SubmitAsync("s1", courseId, 5, new string('x', 2001));

        Assert.False(ok);
        Assert.Contains("2000", error);
    }

    [Fact]
    public async Task Submit_creates_a_review_and_replacing_overwrites_it()
    {
        var (service, db, courseId) = SeedCourseAndEnrollment();

        var created = await service.SubmitAsync("s1", courseId, 4, "  good course  ");
        var replaced = await service.SubmitAsync("s1", courseId, 2, "meh");

        Assert.True(created.Ok);
        Assert.True(replaced.Ok);
        var review = Assert.Single(db.Set<Review>());
        Assert.Equal(2, review.Rating);
        Assert.Equal("meh", review.Comment);
    }

    [Fact]
    public async Task Submit_whitespace_comment_becomes_null()
    {
        var (service, db, courseId) = SeedCourseAndEnrollment();

        var (ok, _) = await service.SubmitAsync("s1", courseId, 5, "   ");

        Assert.True(ok);
        Assert.Null((await db.Set<Review>().SingleAsync()).Comment);
    }

    [Fact]
    public async Task GetRating_returns_zero_when_no_reviews()
    {
        var (service, _, courseId) = SeedCourseAndEnrollment();

        var aggregate = await service.GetRatingAsync(courseId);

        Assert.Equal(0, aggregate.Count);
        Assert.Equal(0d, aggregate.Average);
    }

    [Fact]
    public async Task GetRating_averages_reviews_for_the_course()
    {
        var (service, db, courseId) = SeedCourseAndEnrollment();
        db.Set<Review>().AddRange(
            new Review { CourseId = courseId, UserId = "a", Rating = 4 },
            new Review { CourseId = courseId, UserId = "b", Rating = 2 });
        await db.SaveChangesAsync();

        var aggregate = await service.GetRatingAsync(courseId);

        Assert.Equal(2, aggregate.Count);
        Assert.Equal(3d, aggregate.Average);
    }

    [Fact]
    public async Task GetRatings_returns_empty_for_empty_input()
    {
        var (service, _, _) = SeedCourseAndEnrollment();

        var ratings = await service.GetRatingsAsync(Array.Empty<int>());

        Assert.Empty(ratings);
    }

    [Fact]
    public async Task GetRatings_groups_by_course()
    {
        var (service, db, courseId) = SeedCourseAndEnrollment();
        db.Set<Course>().Add(new Course { Title = "C2", InstructorId = "i1", Status = CourseStatus.Published });
        await db.SaveChangesAsync();
        var course2 = await db.Set<Course>().SingleAsync(c => c.Title == "C2");
        db.Set<Review>().AddRange(
            new Review { CourseId = courseId, UserId = "a", Rating = 5 },
            new Review { CourseId = courseId, UserId = "b", Rating = 3 },
            new Review { CourseId = course2.Id, UserId = "a", Rating = 1 });
        await db.SaveChangesAsync();

        var ratings = await service.GetRatingsAsync(new[] { courseId, course2.Id });

        Assert.Equal(4d, ratings[courseId].Average);
        Assert.Equal(1d, ratings[course2.Id].Average);
    }

    [Fact]
    public async Task Delete_returns_false_for_missing_review()
    {
        var (service, _, _) = SeedCourseAndEnrollment();

        Assert.False(await service.DeleteAsync(999_999));
    }

    [Fact]
    public async Task Delete_removes_the_review()
    {
        var (service, db, courseId) = SeedCourseAndEnrollment();
        var review = new Review { CourseId = courseId, UserId = "s1", Rating = 5 };
        db.Set<Review>().Add(review);
        await db.SaveChangesAsync();

        var deleted = await service.DeleteAsync(review.Id);

        Assert.True(deleted);
        Assert.Empty(db.Set<Review>());
    }
}
