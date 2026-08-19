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
    {
        return _db.Set<Quiz>().AsNoTracking()
                .Include(q => q.Course)
                .Include(q => q.Questions.OrderBy(x => x.OrderIndex))
                    .ThenInclude(x => x.AnswerOptions.OrderBy(o => o.OrderIndex))
                .FirstOrDefaultAsync(q => q.Id == quizId);
    }

    /// <summary>One student answer to a question, shaped per question type.</summary>
    public sealed record QuizAnswerInput(
        int? OptionId,
        string? SelectedOptionIds,
        string? TextAnswer,
        string? FileAnswerUrl);

    public async Task<(int? AttemptId, string? Error)> SubmitAsync(
        string studentId, int quizId, Dictionary<int, QuizAnswerInput> answers)
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

        if (questions.Any(q => !answers.ContainsKey(q.Id)))
        {
            return (null, "Please answer every question before submitting.");
        }

        var attemptAnswers = new List<QuizAttemptAnswer>();
        foreach (var question in questions)
        {
            attemptAnswers.Add(BuildAnswer(question, answers[question.Id]));
        }

        var (score, maxScore) = ComputeScores(attemptAnswers);

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

    /// <summary>
    /// Instructor grades a manual (short-answer / file-upload) answer with a
    /// score and feedback, then the attempt's total score is recalculated.
    /// </summary>
    public async Task<(bool Ok, string? Error)> GradeAsync(int answerId, int score, string? feedback, string graderId)
    {
        var answer = await _db.Set<QuizAttemptAnswer>()
            .Include(a => a.Question)
            .Include(a => a.Attempt)!.ThenInclude(at => at!.Quiz)!.ThenInclude(q => q!.Course)
            .FirstOrDefaultAsync(a => a.Id == answerId);
        if (answer is null)
        {
            return (false, "Answer not found.");
        }

        var question = answer.Question!;
        if (question.QuestionType is not (QuestionType.ShortAnswer or QuestionType.FileUpload))
        {
            return (false, "Only short-answer and file-upload questions are graded manually.");
        }

        if (answer.Attempt?.Quiz?.Course is null || answer.Attempt.Quiz.Course.InstructorId != graderId)
        {
            return (false, "You do not own this course.");
        }

        if (score < 0 || score > question.Points)
        {
            return (false, $"Score must be between 0 and {question.Points}.");
        }

        answer.IsGraded = true;
        answer.GradedScore = score;
        answer.GradingFeedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        await _db.SaveChangesAsync();

        await RecalculateAsync(answer.AttemptId);
        return (true, null);
    }

    /// <summary>Recomputes an attempt's Score/MaxScore from its answers.</summary>
    public async Task RecalculateAsync(int attemptId)
    {
        var attempt = await _db.Set<QuizAttempt>()
            .Include(a => a.Answers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt is null)
        {
            return;
        }

        var (score, maxScore) = ComputeScores(attempt.Answers);
        attempt.Score = score;
        attempt.MaxScore = maxScore;
        await _db.SaveChangesAsync();
    }

    private static QuizAttemptAnswer BuildAnswer(Question question, QuizAnswerInput input)
    {
        var scored = QuestionScoring.Score(
            question, input.OptionId, input.SelectedOptionIds, input.TextAnswer, input.FileAnswerUrl);
        return new QuizAttemptAnswer
        {
            QuestionId = question.Id,
            Question = question,
            AnswerOptionId = scored.OptionId,
            SelectedOptionIds = scored.SelectedOptionIds,
            TextAnswer = scored.TextAnswer,
            FileAnswerUrl = scored.FileAnswerUrl,
            IsCorrect = scored.IsCorrect,
        };
    }

    /// <summary>
    /// Auto-scored objective questions count toward Score/MaxScore immediately;
    /// manual questions only count once graded.
    /// </summary>
    private static (int Score, int MaxScore) ComputeScores(IEnumerable<QuizAttemptAnswer> answers)
    {
        return QuestionScoring.ComputeScores(answers.Select(a => new QuestionScoring.ScoredAnswer(
            Question: a.Question!,
            OptionId: a.AnswerOptionId,
            SelectedOptionIds: a.SelectedOptionIds,
            TextAnswer: a.TextAnswer,
            FileAnswerUrl: a.FileAnswerUrl,
            IsCorrect: a.IsCorrect,
            IsGraded: a.IsGraded,
            GradedScore: a.GradedScore)));
    }

    public Task<List<QuizAttempt>> GetAttemptsForQuizAsync(int quizId, string ownerId)
    {
        return _db.Set<QuizAttempt>().AsNoTracking()
                .Where(a => a.QuizId == quizId && a.Quiz!.Course!.InstructorId == ownerId)
                .Include(a => a.Student)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();
    }

    public Task<List<QuizAttempt>> GetAttemptsForStudentAsync(string studentId, int quizId)
    {
        return _db.Set<QuizAttempt>().AsNoTracking()
                .Where(a => a.StudentId == studentId && a.QuizId == quizId)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();
    }

    /// <summary>For each quiz in a course, the attempt count and best score of one student.</summary>
    public async Task<List<(int Id, string Title, int Attempts, int BestPercent)>> GetQuizzesWithAttemptsForStudentAsync(
        string studentId, int courseId)
    {
        var quizzes = await _db.Set<Quiz>().AsNoTracking()
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.OrderIndex)
            .Select(q => new { q.Id, q.Title })
            .ToListAsync();
        if (quizzes.Count == 0)
        {
            return new List<(int, string, int, int)>();
        }

        var attempts = await _db.Set<QuizAttempt>().AsNoTracking()
            .Where(a => a.StudentId == studentId && quizzes.Select(q => q.Id).Contains(a.QuizId))
            .Select(a => new { a.QuizId, a.Score, a.MaxScore })
            .ToListAsync();

        return quizzes.Select(q => new
        {
            q.Id,
            q.Title,
            Attempts = attempts.Count(a => a.QuizId == q.Id),
            BestPercent = attempts.Where(a => a.QuizId == q.Id && a.MaxScore > 0)
                .Select(a => (int)Math.Round(a.Score * 100.0 / a.MaxScore))
                .DefaultIfEmpty(0)
                .Max(),
        }).Select(x => (x.Id, x.Title, x.Attempts, x.BestPercent)).ToList();
    }

    public Task<QuizAttempt?> GetAttemptAsync(int attemptId, string viewerId)
    {
        return _db.Set<QuizAttempt>().AsNoTracking()
                .Include(a => a.Quiz).ThenInclude(q => q!.Course)
                .Include(a => a.Answers).ThenInclude(x => x.Question).ThenInclude(q => q!.AnswerOptions)
                .FirstOrDefaultAsync(a => a.Id == attemptId
                    && (a.StudentId == viewerId || a.Quiz!.Course!.InstructorId == viewerId));
    }

    /// <summary>Loads one answer with its attempt, scoped to the student or course instructor.</summary>
    public Task<QuizAttemptAnswer?> GetAnswerForAttemptAsync(int answerId, string viewerId)
    {
        return _db.Set<QuizAttemptAnswer>().AsNoTracking()
            .Include(a => a.Attempt)!.ThenInclude(at => at!.Quiz)!.ThenInclude(q => q!.Course)
            .FirstOrDefaultAsync(a => a.Id == answerId
                && (a.Attempt!.StudentId == viewerId || a.Attempt.Quiz!.Course!.InstructorId == viewerId));
    }

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
