using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Assessments;

public sealed class IncorrectAnswerLogTests
{
    private static async Task<(ApplicationDbContext Db, int QuizId, int QuestionId, int CorrectOptionId)> SeedAsync()
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
        var (question, error) = await new QuestionService(db).AddAsync(
            quiz.Id, "i1", "What?", 1, QuestionType.SingleChoice,
            new List<AnswerOptionInput> { new("A", false), new("B", true) });
        Assert.Null(error);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id });
        await db.SaveChangesAsync();
        var correctOptionId = question!.AnswerOptions.Single(o => o.IsCorrect).Id;
        return (db, quiz.Id, question.Id, correctOptionId);
    }

    [Fact]
    public async Task Wrong_answer_is_logged_and_practice_resolves()
    {
        var (db, quizId, questionId, _) = await SeedAsync();
        var incorrect = new IncorrectAnswerService(db);
        var attempts = new AttemptService(db, new EnrollmentService(db), incorrect);

        var wrongOptionId = (await db.Set<Question>().Include(q => q.AnswerOptions).SingleAsync(q => q.Id == questionId))
            .AnswerOptions.Single(o => !o.IsCorrect).Id;

        var (_, error) = await attempts.SubmitAsync("s1", quizId, new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [questionId] = new AttemptService.QuizAnswerInput(wrongOptionId, null, null, null),
        });
        Assert.Null(error);

        var entry = await db.Set<IncorrectAnswer>().SingleAsync(x => x.QuestionId == questionId);
        Assert.Null(entry.ResolvedAt);
        Assert.NotEmpty(entry.ChosenAnswer);

        var practice = await incorrect.BuildPracticeQuestionsAsync("s1");
        Assert.Contains(practice, q => q.Id == questionId);

        await incorrect.ResolveAsync("s1", questionId);
        var resolved = await db.Set<IncorrectAnswer>().SingleAsync(x => x.QuestionId == questionId);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Empty(await incorrect.BuildPracticeQuestionsAsync("s1"));
    }

    [Fact]
    public async Task Correct_answer_in_quiz_is_not_logged()
    {
        var (db, quizId, questionId, correctOptionId) = await SeedAsync();
        var incorrect = new IncorrectAnswerService(db);
        var attempts = new AttemptService(db, new EnrollmentService(db), incorrect);

        var (_, error) = await attempts.SubmitAsync("s1", quizId, new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [questionId] = new AttemptService.QuizAnswerInput(correctOptionId, null, null, null),
        });
        Assert.Null(error);

        Assert.Empty(await db.Set<IncorrectAnswer>().ToListAsync());
    }
}
