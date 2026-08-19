using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Assessments;

public sealed class QuestionTypesTests
{
    private static async Task<(ApplicationDbContext Db, int QuizId)> SeedAsync()
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
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id });
        await db.SaveChangesAsync();
        return (db, quiz.Id);
    }

    private static async Task<int> AddQuestionAsync(
        ApplicationDbContext db, int quizId, QuestionType type, int points = 1, params (string Text, bool IsCorrect)[] options)
    {
        var (question, error) = await new QuestionService(db).AddAsync(
            quizId, "i1", "Question", points, type,
            options.Select(o => new AnswerOptionInput(o.Text, o.IsCorrect)).ToList());
        Assert.Null(error);
        return question!.Id;
    }

    private static async Task<int> GetCorrectOptionIdAsync(ApplicationDbContext db, int questionId)
    {
        var question = await db.Set<Question>().Include(q => q.AnswerOptions).SingleAsync(q => q.Id == questionId);
        return question.AnswerOptions.Single(o => o.IsCorrect).Id;
    }

    private static AttemptService.QuizAnswerInput Option(int? optionId)
    {
        return new(optionId, null, null, null);
    }

    private static AttemptService.QuizAnswerInput Multiple(string ids)
    {
        return new(null, ids, null, null);
    }

    private static AttemptService.QuizAnswerInput Text(string text)
    {
        return new(null, null, text, null);
    }

    [Fact]
    public async Task Submit_scores_all_objective_types()
    {
        var (db, quizId) = await SeedAsync();
        var single = await AddQuestionAsync(db, quizId, QuestionType.SingleChoice, 1, ("A", true), ("B", false));
        var tf = await AddQuestionAsync(db, quizId, QuestionType.TrueFalse, 1, ("True", true), ("False", false));
        var multi = await AddQuestionAsync(db, quizId, QuestionType.MultipleChoice, 1, ("A", true), ("B", true), ("C", false));
        var fill = await AddQuestionAsync(db, quizId, QuestionType.FillBlank, 1, ("Paris", true));

        var multiQuestion = await db.Set<Question>().Include(q => q.AnswerOptions).SingleAsync(q => q.Id == multi);
        var correctIds = string.Join(",", multiQuestion.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id));

        var attempts = new AttemptService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var (id, error) = await attempts.SubmitAsync("s1", quizId, new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [single] = Option(await GetCorrectOptionIdAsync(db, single)),
            [tf] = Option(await GetCorrectOptionIdAsync(db, tf)),
            [multi] = Multiple(correctIds),
            [fill] = Text("  pArIs "),
        });

        Assert.Null(error);
        var attempt = await db.Set<QuizAttempt>().Include(a => a.Answers).SingleAsync(a => a.Id == id);
        Assert.Equal(4, attempt.Score);
        Assert.Equal(4, attempt.MaxScore);
        Assert.All(attempt.Answers, a => Assert.True(a.IsCorrect));
    }

    [Fact]
    public async Task Submit_marks_multiple_choice_wrong_when_selection_mismatches()
    {
        var (db, quizId) = await SeedAsync();
        var multi = await AddQuestionAsync(db, quizId, QuestionType.MultipleChoice, 1, ("A", true), ("B", true), ("C", false));
        var multiQuestion = await db.Set<Question>().Include(q => q.AnswerOptions).SingleAsync(q => q.Id == multi);
        var onlyA = multiQuestion.AnswerOptions.Single(o => o.Text == "A").Id;

        var attempts = new AttemptService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var (id, _) = await attempts.SubmitAsync("s1", quizId, new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [multi] = Multiple(onlyA.ToString(CultureInfo.InvariantCulture)),
        });

        var answer = await db.Set<QuizAttemptAnswer>().SingleAsync(a => a.AttemptId == id);
        Assert.False(answer.IsCorrect);
        Assert.Equal(0, (await db.Set<QuizAttempt>().SingleAsync(a => a.Id == id)).Score);
    }

    [Fact]
    public async Task Submit_excludes_manual_answers_until_graded()
    {
        var (db, quizId) = await SeedAsync();
        var single = await AddQuestionAsync(db, quizId, QuestionType.SingleChoice, 1, ("A", true), ("B", false));
        var shortAnswer = await AddQuestionAsync(db, quizId, QuestionType.ShortAnswer, 4);

        var attempts = new AttemptService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var (id, error) = await attempts.SubmitAsync("s1", quizId, new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [single] = Option(await GetCorrectOptionIdAsync(db, single)),
            [shortAnswer] = Text("my essay text"),
        });

        Assert.Null(error);
        var attempt = await db.Set<QuizAttempt>().Include(a => a.Answers).SingleAsync(a => a.Id == id);
        Assert.Equal(1, attempt.Score);      // only the objective question
        Assert.Equal(1, attempt.MaxScore);   // manual excluded
        var manual = attempt.Answers.Single(a => a.QuestionId == shortAnswer);
        Assert.False(manual.IsGraded);
        Assert.Equal("my essay text", manual.TextAnswer);
    }

    [Fact]
    public async Task Grade_manual_answer_recalculates_attempt_totals()
    {
        var (db, quizId) = await SeedAsync();
        var single = await AddQuestionAsync(db, quizId, QuestionType.SingleChoice, 1, ("A", true), ("B", false));
        var shortAnswer = await AddQuestionAsync(db, quizId, QuestionType.ShortAnswer, 4);
        var attempts = new AttemptService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var (id, _) = await attempts.SubmitAsync("s1", quizId, new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [single] = Option(await GetCorrectOptionIdAsync(db, single)),
            [shortAnswer] = Text("essay"),
        });
        var manual = await db.Set<QuizAttemptAnswer>().SingleAsync(a => a.AttemptId == id && a.QuestionId == shortAnswer);

        var (ok, error) = await attempts.GradeAsync(manual.Id, 3, "Well written", "i1");
        Assert.True(ok);
        Assert.Null(error);

        var updated = await db.Set<QuizAttempt>().SingleAsync(a => a.Id == id);
        Assert.Equal(4, updated.Score);      // 1 auto + 3 manual
        Assert.Equal(5, updated.MaxScore);   // 1 + 4

        Assert.False((await attempts.GradeAsync(manual.Id, 3, "x", "other")).Ok); // not the owner
        Assert.False((await attempts.GradeAsync(manual.Id, 99, "x", "i1")).Ok);   // out of range
    }

    [Fact]
    public async Task QuestionService_validates_options_per_type()
    {
        var (db, quizId) = await SeedAsync();
        var service = new QuestionService(db);

        Assert.NotNull((await service.AddAsync(quizId, "i1", "Q", 1, QuestionType.ShortAnswer,
            new List<AnswerOptionInput> { new("x", true) })).Error); // options on manual type
        Assert.NotNull((await service.AddAsync(quizId, "i1", "Q", 1, QuestionType.SingleChoice,
            new List<AnswerOptionInput> { new("A", false), new("B", false) })).Error); // no correct
        Assert.NotNull((await service.AddAsync(quizId, "i1", "Q", 1, QuestionType.MultipleChoice,
            new List<AnswerOptionInput> { new("A", false), new("B", false) })).Error); // no correct
        Assert.NotNull((await service.AddAsync(quizId, "i1", "Q", 1, QuestionType.FillBlank,
            new List<AnswerOptionInput> { new("x", false) })).Error); // no acceptable
        Assert.Null((await service.AddAsync(quizId, "i1", "Q", 1, QuestionType.MultipleChoice,
            new List<AnswerOptionInput> { new("A", true), new("B", true) })).Error); // multiple correct ok
    }
}
