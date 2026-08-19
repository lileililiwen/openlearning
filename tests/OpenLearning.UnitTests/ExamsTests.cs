using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Exams;

public sealed class ExamsTests
{
    private static async Task<(ApplicationDbContext Db, int ExamId)> SeedAsync(int maxAttempts = 3, int passPercent = 60)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        var exam = new Exam
        {
            CourseId = course.Id,
            AuthorId = "i1",
            Title = "E",
            MaxAttempts = maxAttempts,
            PassPercent = passPercent,
            DurationMinutes = 30,
        };
        db.Set<Exam>().Add(exam);
        await db.SaveChangesAsync();
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id });
        await db.SaveChangesAsync();
        return (db, exam.Id);
    }

    private static async Task<int> AddQuestionAsync(ApplicationDbContext db, int examId, params (string Text, bool IsCorrect)[] options)
    {
        var (question, error) = await new ExamService(db, new EnrollmentService(db), new IncorrectAnswerService(db)).AddQuestionAsync(
            examId, "i1", "Question", 1, QuestionType.SingleChoice,
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

    [Fact]
    public async Task Submit_scores_marks_passed_and_records_switches()
    {
        var (db, examId) = await SeedAsync(passPercent: 60);
        var exams = new ExamService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var q1 = await AddQuestionAsync(db, examId, ("A", true), ("B", false));
        var q2 = await AddQuestionAsync(db, examId, ("C", true), ("D", false));

        var start = await exams.StartAsync(examId, "s1");
        Assert.Null(start.Error);
        Assert.NotNull(start.Attempt);
        var attempt = start.Attempt;

        var (id, submitError) = await exams.SubmitAsync(attempt.Id, "s1", new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [q1] = Option(await GetCorrectOptionIdAsync(db, q1)),
            [q2] = Option(await GetCorrectOptionIdAsync(db, q2)),
        }, 1);

        Assert.Null(submitError);
        var saved = await db.Set<ExamAttempt>().Include(a => a.Answers).SingleAsync(a => a.Id == id);
        Assert.Equal(2, saved.Score);
        Assert.Equal(2, saved.MaxScore);
        Assert.Equal(100, saved.Percent);
        Assert.True(saved.Passed);
        Assert.Equal(1, saved.ScreenSwitchCount);
        Assert.Equal(ExamAttemptStatus.Completed, saved.Status);
        Assert.All(saved.Answers, a => Assert.True(a.IsCorrect));
    }

    [Fact]
    public async Task StartAsync_denies_after_using_all_attempts()
    {
        var (db, examId) = await SeedAsync(maxAttempts: 1);
        var exams = new ExamService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var q1 = await AddQuestionAsync(db, examId, ("A", true), ("B", false));

        var firstStart = await exams.StartAsync(examId, "s1");
        Assert.Null(firstStart.Error);
        Assert.NotNull(firstStart.Attempt);
        var first = firstStart.Attempt;

        var (_, submitError) = await exams.SubmitAsync(first.Id, "s1", new Dictionary<int, AttemptService.QuizAnswerInput>
        {
            [q1] = Option(await GetCorrectOptionIdAsync(db, q1)),
        }, 0);
        Assert.Null(submitError);

        var (second, limitError) = await exams.StartAsync(examId, "s1");
        Assert.Null(second);
        Assert.NotNull(limitError);
        Assert.Contains("attempt", limitError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartAsync_denies_before_opening()
    {
        var (db, examId) = await SeedAsync();
        var exam = await db.Set<Exam>().SingleAsync(e => e.Id == examId);
        exam.OpensAt = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync();

        var exams = new ExamService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var (attempt, error) = await exams.StartAsync(examId, "s1");
        Assert.Null(attempt);
        Assert.NotNull(error);
        Assert.Contains("not opened", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Non_owner_cannot_create_question()
    {
        var (db, examId) = await SeedAsync();
        var exams = new ExamService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var (_, error) = await exams.AddQuestionAsync(
            examId, "other", "Q", 1, QuestionType.SingleChoice,
            new List<AnswerOptionInput> { new("A", true), new("B", false) });
        Assert.NotNull(error);
        Assert.Contains("own", error, StringComparison.OrdinalIgnoreCase);
    }
}
