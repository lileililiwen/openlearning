using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.Assessments;

public sealed class QuestionBankTests
{
    private static async Task<(ApplicationDbContext Db, int CourseId, int QuizId)> SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        var quiz = new Quiz { CourseId = course.Id, Title = "Q", OrderIndex = 1 };
        db.Set<Quiz>().Add(quiz);
        await db.SaveChangesAsync();
        return (db, course.Id, quiz.Id);
    }

    [Fact]
    public async Task Create_search_and_import_produce_snapshot()
    {
        var (db, _, quizId) = await SeedAsync();
        var bank = new QuestionBankService(db);

        var (created, error) = await bank.CreateAsync(
            "What is 2+2?", 1, QuestionType.SingleChoice, "Math",
            new List<AnswerOptionInput> { new("3", false), new("4", true) });
        Assert.Null(error);
        Assert.NotNull(created);

        var (items, total) = await bank.SearchAsync(null, "2+2", 1, 20);
        Assert.Equal(1, total);
        Assert.Single(items);

        var (ok, importError) = await bank.ImportIntoQuizAsync(created.Id, quizId, "i1");
        Assert.True(ok);
        Assert.Null(importError);

        var copy = await db.Set<Question>()
            .Include(q => q.AnswerOptions)
            .SingleAsync(q => q.QuizId == quizId);
        Assert.False(copy.IsBank);
        Assert.Equal("What is 2+2?", copy.Text);
        Assert.Equal(2, copy.AnswerOptions.Count);

        // Editing the bank question must not touch the imported copy.
        await bank.UpdateAsync(created.Id, "Edited?", 2, QuestionType.SingleChoice, "Math",
            new List<AnswerOptionInput> { new("A", true), new("B", false) });
        var refreshed = await db.Set<Question>().SingleAsync(q => q.Id == copy.Id);
        Assert.Equal("What is 2+2?", refreshed.Text);
    }

    [Fact]
    public async Task Import_requires_quiz_ownership()
    {
        var (db, _, quizId) = await SeedAsync();
        var bank = new QuestionBankService(db);
        var (created, _) = await bank.CreateAsync(
            "Q", 1, QuestionType.SingleChoice, null,
            new List<AnswerOptionInput> { new("A", true), new("B", false) });
        Assert.NotNull(created);

        var (ok, error) = await bank.ImportIntoQuizAsync(created.Id, quizId, "other");
        Assert.False(ok);
        Assert.NotNull(error);
    }
}
