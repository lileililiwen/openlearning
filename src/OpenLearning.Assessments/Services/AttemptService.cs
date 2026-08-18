using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Assessments.Services;

public class AttemptService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;

    public AttemptService(DbContext db, EnrollmentService enrollments)
    {
        _db = db;
        _enrollments = enrollments;
    }

    public Task<Quiz?> GetQuizForTakeAsync(int quizId)
        => _db.Set<Quiz>().AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Questions.OrderBy(x => x.OrderIndex))
                .ThenInclude(x => x.AnswerOptions.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == quizId);

    public async Task<(int? AttemptId, string? Error)> SubmitAsync(
        string studentId, int quizId, Dictionary<int, int> answers)
    {
        var quiz = await _db.Set<Quiz>()
            .Include(q => q.Questions.OrderBy(x => x.OrderIndex))
                .ThenInclude(x => x.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null)
        {
            return (null, "Quiz not found.");
        }

        var enrolled = await _enrollments.IsEnrolledAsync(studentId, quiz.CourseId);
        if (!enrolled)
        {
            return (null, "You must be enrolled in this course to take the quiz.");
        }

        var questions = quiz.Questions.OrderBy(q => q.OrderIndex).ToList();
        if (questions.Count == 0)
        {
            return (null, "This quiz has no questions yet.");
        }

        foreach (var question in questions)
        {
            if (!answers.ContainsKey(question.Id))
            {
                return (null, "Please answer every question before submitting.");
            }
        }

        var maxScore = questions.Sum(q => q.Points);
        var score = 0;
        var attemptAnswers = new List<QuizAttemptAnswer>();

        foreach (var question in questions)
        {
            var selectedOptionId = answers[question.Id];
            var isCorrect = question.AnswerOptions.Any(o => o.Id == selectedOptionId && o.IsCorrect);
            if (isCorrect)
            {
                score += question.Points;
            }

            attemptAnswers.Add(new QuizAttemptAnswer
            {
                QuestionId = question.Id,
                AnswerOptionId = selectedOptionId,
                IsCorrect = isCorrect,
            });
        }

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            StudentId = studentId,
            Score = score,
            MaxScore = maxScore,
            Answers = attemptAnswers,
        };

        _db.Set<QuizAttempt>().Add(attempt);
        await _db.SaveChangesAsync();
        return (attempt.Id, null);
    }

    public Task<List<QuizAttempt>> GetAttemptsForQuizAsync(int quizId, string ownerId)
        => _db.Set<QuizAttempt>().AsNoTracking()
            .Where(a => a.QuizId == quizId && a.Quiz!.Course!.InstructorId == ownerId)
            .Include(a => a.Student)
            .OrderByDescending(a => a.CompletedAt)
            .ToListAsync();

    public Task<List<QuizAttempt>> GetAttemptsForStudentAsync(string studentId, int quizId)
        => _db.Set<QuizAttempt>().AsNoTracking()
            .Where(a => a.StudentId == studentId && a.QuizId == quizId)
            .OrderByDescending(a => a.CompletedAt)
            .ToListAsync();

    public Task<QuizAttempt?> GetAttemptAsync(int attemptId, string viewerId)
        => _db.Set<QuizAttempt>().AsNoTracking()
            .Include(a => a.Quiz).ThenInclude(q => q!.Course)
            .Include(a => a.Answers).ThenInclude(x => x.Question).ThenInclude(q => q!.AnswerOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId
                && (a.StudentId == viewerId || a.Quiz!.Course!.InstructorId == viewerId));

    /// <summary>
    /// Number of quizzes in a course vs how many distinct quizzes the Student
    /// has attempted at least once.
    /// </summary>
    public async Task<(int TotalQuizzes, int AttemptedQuizzes)> GetQuizStatusAsync(string studentId, int courseId)
    {
        var total = await _db.Set<Quiz>().CountAsync(q => q.CourseId == courseId);
        var attempted = await _db.Set<QuizAttempt>()
            .Where(a => a.StudentId == studentId && a.Quiz!.CourseId == courseId)
            .Select(a => a.QuizId)
            .Distinct()
            .CountAsync();
        return (total, attempted);
    }

    /// <summary>
    /// Percentage of quiz attempts in a course that scored at least 70% of the
    /// maximum (the platform's default pass threshold); null when no attempts
    /// exist yet.
    /// </summary>
    public async Task<int?> GetCourseQuizPassRateAsync(int courseId)
    {
        var attempts = await _db.Set<QuizAttempt>().AsNoTracking()
            .Where(a => a.Quiz!.CourseId == courseId)
            .Select(a => new { a.Score, a.MaxScore })
            .ToListAsync();
        if (attempts.Count == 0)
        {
            return null;
        }

        const double passThreshold = 0.7;
        var passed = attempts.Count(a => a.MaxScore > 0 && a.Score * 1.0 / a.MaxScore >= passThreshold);
        return (int)Math.Round(passed * 100.0 / attempts.Count);
    }
}
