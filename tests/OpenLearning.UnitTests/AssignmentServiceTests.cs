using Microsoft.EntityFrameworkCore;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.Assignments;

public sealed class AssignmentServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<int> CreateAssignmentAsync(
        ApplicationDbContext db, int courseId = 1, string authorId = "instructor-1", bool allowResubmit = false)
    {
        var service = new AssignmentService(db);
        var (ok, error) = await service.CreateAsync(
            courseId, authorId, "Essay", "Write 500 words", null, allowResubmit);
        Assert.True(ok);
        Assert.Null(error);
        return (await service.GetForCourseAsync(courseId))[0].Id;
    }

    [Fact]
    public async Task CreateAsync_and_GetForCourseAsync_roundtrip()
    {
        var db = CreateDb();
        var id = await CreateAssignmentAsync(db);

        var list = await db.Set<Assignment>().ToListAsync();
        Assert.Single(list);
        Assert.Equal("Essay", list[0].Title);
        Assert.Equal("instructor-1", (await db.Set<Assignment>().FindAsync(id))!.AuthorId);
    }

    [Fact]
    public async Task UpdateAsync_requires_owner()
    {
        var db = CreateDb();
        var id = await CreateAssignmentAsync(db);
        var service = new AssignmentService(db);

        var (ok, error) = await service.UpdateAsync(id, "other-instructor", "Essay", "x", null, false);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SubmitAsync_creates_and_replaces_submission()
    {
        var db = CreateDb();
        var assignmentId = await CreateAssignmentAsync(db);
        var service = new AssignmentService(db);

        var (ok1, _) = await service.SubmitAsync(assignmentId, "student-1", "draft", null);
        Assert.True(ok1);
        var (ok2, _) = await service.SubmitAsync(assignmentId, "student-1", "final", null);
        Assert.True(ok2);

        var submission = await service.GetSubmissionAsync(assignmentId, "student-1");
        Assert.NotNull(submission);
        Assert.Equal("final", submission.Text);
        Assert.Single(db.Set<AssignmentSubmission>()); // replaced, not duplicated
    }

    [Fact]
    public async Task SubmitAsync_rejects_resubmit_after_grading_when_disallowed()
    {
        var db = CreateDb();
        var assignmentId = await CreateAssignmentAsync(db);
        var service = new AssignmentService(db);
        await service.SubmitAsync(assignmentId, "student-1", "first", null);
        var submission = await service.GetSubmissionAsync(assignmentId, "student-1");
        await service.GradeAsync(submission!.Id, "instructor-1", 80, "Good");

        var (ok, error) = await service.SubmitAsync(assignmentId, "student-1", "second", null);

        Assert.False(ok);
        Assert.Contains("not allowed", error, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_allows_resubmit_after_grading_when_enabled()
    {
        var db = CreateDb();
        var assignmentId = await CreateAssignmentAsync(db, allowResubmit: true);
        var service = new AssignmentService(db);
        await service.SubmitAsync(assignmentId, "student-1", "first", null);
        var submission = await service.GetSubmissionAsync(assignmentId, "student-1");
        await service.GradeAsync(submission!.Id, "instructor-1", 80, "Good");

        var (ok, error) = await service.SubmitAsync(assignmentId, "student-1", "second", null);

        Assert.True(ok);
        Assert.Null(error);
        var updated = await service.GetSubmissionAsync(assignmentId, "student-1");
        Assert.Equal("second", updated!.Text);
        Assert.Null(updated.GradedAt); // grading reset on resubmit
    }

    [Fact]
    public async Task GradeAsync_sets_score_feedback_and_validates_range()
    {
        var db = CreateDb();
        var assignmentId = await CreateAssignmentAsync(db);
        var service = new AssignmentService(db);
        await service.SubmitAsync(assignmentId, "student-1", "work", null);
        var submission = await service.GetSubmissionAsync(assignmentId, "student-1");
        Assert.NotNull(submission);

        var (bad, badError) = await service.GradeAsync(submission.Id, "instructor-1", 150, null);
        Assert.False(bad);
        Assert.NotNull(badError);

        var (ok, error) = await service.GradeAsync(submission.Id, "instructor-1", 85, "Well done");
        Assert.True(ok);
        Assert.Null(error);
        var graded = await service.GetSubmissionAsync(assignmentId, "student-1");
        Assert.Equal(85, graded!.Score);
        Assert.Equal("Well done", graded.Feedback);
        Assert.NotNull(graded.GradedAt);
    }
}
