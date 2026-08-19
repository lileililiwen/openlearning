using Microsoft.EntityFrameworkCore;
using OpenLearning.Community.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Community;

public sealed class CommunityTests
{
    private static async Task<(ApplicationDbContext Db, int CourseId)> SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id });
        await db.SaveChangesAsync();
        return (db, course.Id);
    }

    [Fact]
    public async Task Ask_reply_and_duplicate_guard()
    {
        var (db, courseId) = await SeedAsync();
        var service = new CommunityService(db);

        var (ok, error) = await service.AskAsync(courseId, "s1", "How to start?", "Body", null, isAdmin: false);
        Assert.True(ok);
        Assert.Null(error);

        var question = await db.Set<OpenLearning.Community.Models.Question>().SingleAsync();
        Assert.Equal("s1", question.AuthorId);

        Assert.True((await service.ReplyToQuestionAsync(question.Id, "i1", "Read the docs.", isAdmin: false)).Ok);
        var (dupOk, dupError) = await service.ReplyToQuestionAsync(question.Id, "i1", "Read the docs.", isAdmin: false);
        Assert.False(dupOk);
        Assert.NotNull(dupError);
    }

    [Fact]
    public async Task Non_enrolled_cannot_ask()
    {
        var (db, courseId) = await SeedAsync();
        var service = new CommunityService(db);

        var (ok, error) = await service.AskAsync(courseId, "outsider", "Hi?", "Body", null, isAdmin: false);
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Admin_can_delete()
    {
        var (db, courseId) = await SeedAsync();
        var service = new CommunityService(db);
        await service.CreatePostAsync(courseId, "s1", "Hello everyone", null, isAdmin: false);
        var post = await db.Set<OpenLearning.Community.Models.Post>().SingleAsync();

        Assert.True(await service.DeletePostAsync(post.Id));
        Assert.Empty(await db.Set<OpenLearning.Community.Models.Post>().ToListAsync());
    }
}
