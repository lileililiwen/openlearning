using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Exams.Models;

namespace OpenLearning.Exams.Services;

/// <summary>Editable exam settings; used by create and update.</summary>
public sealed record ExamDraft(
    string Title,
    string Description,
    bool IsOfficial,
    int DurationMinutes,
    int PassPercent,
    int MaxAttempts,
    DateTime? OpensAt,
    DateTime? ClosesAt);

/// <summary>
/// Exams: owner-gated CRUD, timed taking with attempt limits, scoring that
/// reuses the assessments question-types logic, and reviewable results.
/// </summary>
public class ExamService
{
    /// <summary>Switching away from the exam page this many times auto-submits.</summary>
    public const int MaxScreenSwitches = 3;

    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;

    public ExamService(DbContext db, EnrollmentService enrollments)
    {
        _db = db;
        _enrollments = enrollments;
    }

    // ===== CRUD (owner-gated) =====

    public Task<List<Exam>> GetForCourseAsync(int courseId)
    {
        return _db.Set<Exam>().AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .OrderBy(e => e.IsOfficial)
                .ThenBy(e => e.Title)
                .Include(e => e.Questions)
                .ToListAsync();
    }

    public Task<Exam?> GetByIdAsync(int id)
    {
        return _db.Set<Exam>().AsNoTracking()
                .Include(e => e.Course)
                .Include(e => e.Questions.OrderBy(q => q.OrderIndex))
                    .ThenInclude(q => q.AnswerOptions.OrderBy(o => o.OrderIndex))
                .FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<bool> IsOwnerAsync(int examId, string userId)
    {
        return _db.Set<Exam>().AsNoTracking()
                .AnyAsync(e => e.Id == examId && e.Course!.InstructorId == userId);
    }

    public Task<bool> IsCourseOwnerAsync(int courseId, string userId)
    {
        return _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    public async Task<Exam?> CreateAsync(int courseId, string authorId, ExamDraft draft)
    {
        if (!await IsCourseOwnerAsync(courseId, authorId))
        {
            return null;
        }

        var exam = new Exam
        {
            CourseId = courseId,
            AuthorId = authorId,
            Title = draft.Title,
            Description = draft.Description,
            IsOfficial = draft.IsOfficial,
            DurationMinutes = draft.DurationMinutes,
            PassPercent = draft.PassPercent,
            MaxAttempts = draft.MaxAttempts,
            OpensAt = draft.OpensAt,
            ClosesAt = draft.ClosesAt,
        };
        _db.Set<Exam>().Add(exam);
        await _db.SaveChangesAsync();
        return exam;
    }

    public async Task<bool> UpdateAsync(int examId, string ownerId, ExamDraft draft)
    {
        var exam = await _db.Set<Exam>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam?.Course is null || exam.Course.InstructorId != ownerId)
        {
            return false;
        }

        exam.Title = draft.Title;
        exam.Description = draft.Description;
        exam.IsOfficial = draft.IsOfficial;
        exam.DurationMinutes = draft.DurationMinutes;
        exam.PassPercent = draft.PassPercent;
        exam.MaxAttempts = draft.MaxAttempts;
        exam.OpensAt = draft.OpensAt;
        exam.ClosesAt = draft.ClosesAt;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int examId, string ownerId)
    {
        var exam = await _db.Set<Exam>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam?.Course is null || exam.Course.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<Exam>().Remove(exam);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== Question management (owner-gated) =====

    public Task<Question?> GetQuestionAsync(int questionId)
    {
        return _db.Set<Question>().AsNoTracking()
                .Include(q => q.AnswerOptions.OrderBy(o => o.OrderIndex))
                .FirstOrDefaultAsync(q => q.Id == questionId);
    }

    public async Task<(Question? Question, string? Error)> AddQuestionAsync(
        int examId, string ownerId, string text, int points, QuestionType questionType, List<AnswerOptionInput> options)
    {
        if (!QuestionService.TryValidateOptions(questionType, options, out var validationError))
        {
            return (null, validationError);
        }

        var exam = await _db.Set<Exam>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam?.Course is null || exam.Course.InstructorId != ownerId)
        {
            return (null, "You do not own this course.");
        }

        var nextOrder = await _db.Set<Question>()
            .Where(q => q.ExamId == examId)
            .Select(q => (int?)q.OrderIndex)
            .MaxAsync() ?? 0;

        var question = new Question
        {
            ExamId = examId,
            Text = text,
            QuestionType = questionType,
            OrderIndex = nextOrder + 1,
            Points = points,
        };
        for (var i = 0; i < options.Count; i++)
        {
            question.AnswerOptions.Add(new AnswerOption
            {
                Text = options[i].Text,
                IsCorrect = options[i].IsCorrect,
                OrderIndex = i + 1,
            });
        }

        _db.Set<Question>().Add(question);
        await _db.SaveChangesAsync();
        return (question, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateQuestionAsync(
        int questionId, string ownerId, string text, int points, QuestionType questionType, List<AnswerOptionInput> options)
    {
        if (!QuestionService.TryValidateOptions(questionType, options, out var validationError))
        {
            return (false, validationError);
        }

        var question = await _db.Set<Question>()
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question is null)
        {
            return (false, "Question not found.");
        }

        if (!await IsOwnerAsync(question.ExamId!.Value, ownerId))
        {
            return (false, "You do not own this course.");
        }

        question.Text = text;
        question.Points = points;
        question.QuestionType = questionType;
        _db.Set<AnswerOption>().RemoveRange(question.AnswerOptions);
        question.AnswerOptions.Clear();
        for (var i = 0; i < options.Count; i++)
        {
            question.AnswerOptions.Add(new AnswerOption
            {
                Text = options[i].Text,
                IsCorrect = options[i].IsCorrect,
                OrderIndex = i + 1,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteQuestionAsync(int questionId, string ownerId)
    {
        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question is null || question.ExamId is null)
        {
            return false;
        }

        if (!await IsOwnerAsync(question.ExamId.Value, ownerId))
        {
            return false;
        }

        _db.Set<Question>().Remove(question);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== Taking =====

    public Task<Exam?> GetForTakeAsync(int examId)
    {
        return _db.Set<Exam>().AsNoTracking()
                .Include(e => e.Course)
                .Include(e => e.Questions.OrderBy(q => q.OrderIndex))
                    .ThenInclude(q => q.AnswerOptions.OrderBy(o => o.OrderIndex))
                .FirstOrDefaultAsync(e => e.Id == examId);
    }

    /// <summary>
    /// Starts (or resumes) an attempt, enforcing the availability window and
    /// the per-student attempt limit. Expired in-progress attempts are finalized
    /// as zero-score submissions before the limit is checked.
    /// </summary>
    public async Task<(ExamAttempt? Attempt, string? Error)> StartAsync(int examId, string studentId)
    {
        var exam = await _db.Set<Exam>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam is null)
        {
            return (null, "Exam not found.");
        }

        if (exam.Course is null || !await _enrollments.IsEnrolledAsync(studentId, exam.CourseId))
        {
            return (null, "You must be enrolled in this course to take the exam.");
        }

        var now = DateTime.UtcNow;
        if (exam.OpensAt.HasValue && now < exam.OpensAt.Value)
        {
            return (null, "This exam has not opened yet.");
        }

        if (exam.ClosesAt.HasValue && now > exam.ClosesAt.Value)
        {
            return (null, "This exam has closed.");
        }

        // Resume a live in-progress attempt instead of burning a fresh one.
        var inProgress = await _db.Set<ExamAttempt>()
            .Where(a => a.ExamId == examId && a.StudentId == studentId && a.Status == ExamAttemptStatus.InProgress)
            .ToListAsync();
        var live = inProgress.FirstOrDefault(a => a.StartedAt.AddMinutes(exam.DurationMinutes) > now);
        if (live is not null)
        {
            return (live, null);
        }

        foreach (var stale in inProgress)
        {
            stale.Status = ExamAttemptStatus.Completed;
            stale.SubmittedAt = now;
        }

        if (inProgress.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        var used = await _db.Set<ExamAttempt>()
            .CountAsync(a => a.ExamId == examId && a.StudentId == studentId);
        if (used >= exam.MaxAttempts)
        {
            return (null, $"You have used all {exam.MaxAttempts} allowed attempt(s) for this exam.");
        }

        var attempt = new ExamAttempt { ExamId = examId, StudentId = studentId };
        _db.Set<ExamAttempt>().Add(attempt);
        await _db.SaveChangesAsync();
        return (attempt, null);
    }

    /// <summary>
    /// Finalizes an in-progress attempt with the submitted answers and the
    /// recorded screen-switch count, computing the auto score. Manual questions
    /// are excluded from the score until graded (aligned with question-types).
    /// </summary>
    public async Task<(int? AttemptId, string? Error)> SubmitAsync(
        int attemptId, string studentId, Dictionary<int, AttemptService.QuizAnswerInput> answers, int screenSwitchCount)
    {
        var attempt = await _db.Set<ExamAttempt>()
            .FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt is null)
        {
            return (null, "Attempt not found.");
        }

        if (attempt.StudentId != studentId)
        {
            return (null, "This attempt does not belong to you.");
        }

        if (attempt.Status == ExamAttemptStatus.Completed)
        {
            return (null, "This attempt was already submitted.");
        }

        var exam = await _db.Set<Exam>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == attempt.ExamId);
        if (exam is null)
        {
            return (null, "Exam not found.");
        }

        var questions = await _db.Set<Question>()
            .Where(q => q.ExamId == attempt.ExamId)
            .OrderBy(q => q.OrderIndex)
            .Include(q => q.AnswerOptions)
            .ToListAsync();
        if (questions.Count == 0)
        {
            return (null, "This exam has no questions.");
        }

        if (questions.Any(q => !answers.ContainsKey(q.Id)))
        {
            return (null, "Please answer every question before submitting.");
        }

        var attemptAnswers = new List<ExamAttemptAnswer>();
        foreach (var question in questions)
        {
            var input = answers[question.Id];
            var scored = QuestionScoring.Score(
                question, input.OptionId, input.SelectedOptionIds, input.TextAnswer, input.FileAnswerUrl);
            attemptAnswers.Add(new ExamAttemptAnswer
            {
                QuestionId = question.Id,
                Question = question,
                AnswerOptionId = scored.OptionId,
                SelectedOptionIds = scored.SelectedOptionIds,
                TextAnswer = scored.TextAnswer,
                FileAnswerUrl = scored.FileAnswerUrl,
                IsCorrect = scored.IsCorrect,
            });
        }

        var scoredAnswers = attemptAnswers.Select(a => new QuestionScoring.ScoredAnswer(
            Question: a.Question!,
            OptionId: a.AnswerOptionId,
            SelectedOptionIds: a.SelectedOptionIds,
            TextAnswer: a.TextAnswer,
            FileAnswerUrl: a.FileAnswerUrl,
            IsCorrect: a.IsCorrect));
        var (score, maxScore) = QuestionScoring.ComputeScores(scoredAnswers);

        attempt.Answers = attemptAnswers;
        attempt.Score = score;
        attempt.MaxScore = maxScore;
        attempt.Percent = maxScore > 0 ? (int)Math.Round(score * 100.0 / maxScore) : 0;
        attempt.Passed = attempt.Percent >= exam.PassPercent;
        attempt.ScreenSwitchCount = Math.Max(0, screenSwitchCount);
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Status = ExamAttemptStatus.Completed;
        await _db.SaveChangesAsync();
        return (attempt.Id, null);
    }

    /// <summary>Copies a question from the central bank into this exam (snapshot).</summary>
    public async Task<(bool Ok, string? Error)> ImportFromBankAsync(int bankQuestionId, int examId, string ownerId)
    {
        var exam = await _db.Set<Exam>()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam?.Course is null || exam.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        var source = await _db.Set<Question>().AsNoTracking()
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == bankQuestionId && q.IsBank && q.ArchivedAt == null);
        if (source is null)
        {
            return (false, "Bank question not found.");
        }

        var nextOrder = await _db.Set<Question>()
            .Where(q => q.ExamId == examId)
            .Select(q => (int?)q.OrderIndex)
            .MaxAsync() ?? 0;

        var copy = new Question
        {
            ExamId = examId,
            Text = source.Text,
            QuestionType = source.QuestionType,
            Points = source.Points,
            OrderIndex = nextOrder + 1,
        };
        foreach (var option in source.AnswerOptions.OrderBy(o => o.OrderIndex))
        {
            copy.AnswerOptions.Add(new AnswerOption
            {
                Text = option.Text,
                IsCorrect = option.IsCorrect,
                OrderIndex = option.OrderIndex,
            });
        }

        _db.Set<Question>().Add(copy);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Results & grading =====

    public Task<ExamAttempt?> GetAttemptAsync(int attemptId, string viewerId)
    {
        return _db.Set<ExamAttempt>().AsNoTracking()
                .Include(a => a.Exam).ThenInclude(e => e!.Course)
                .Include(a => a.Student)
                .Include(a => a.Answers).ThenInclude(x => x.Question).ThenInclude(q => q!.AnswerOptions)
                .FirstOrDefaultAsync(a => a.Id == attemptId
                    && (a.StudentId == viewerId || a.Exam!.Course!.InstructorId == viewerId));
    }

    /// <summary>All attempts for an exam, scoped to the owning instructor.</summary>
    public Task<List<ExamAttempt>> GetAttemptsForInstructorAsync(int examId, string ownerId)
    {
        return _db.Set<ExamAttempt>().AsNoTracking()
                .Where(a => a.ExamId == examId && a.Exam!.Course!.InstructorId == ownerId)
                .Include(a => a.Student)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();
    }

    public Task<int> GetAttemptCountAsync(int examId, string studentId)
    {
        return _db.Set<ExamAttempt>().CountAsync(a => a.ExamId == examId && a.StudentId == studentId);
    }

    /// <summary>Completed attempts for the same student/exam that finished before a given attempt.</summary>
    public Task<int> GetPriorCompletedCountAsync(int examId, string studentId, int attemptId)
    {
        return _db.Set<ExamAttempt>()
            .CountAsync(a => a.ExamId == examId && a.StudentId == studentId && a.Id < attemptId);
    }

    /// <summary>Loads one answer with its attempt, scoped to the student or course instructor.</summary>
    public Task<ExamAttemptAnswer?> GetAnswerForAttemptAsync(int answerId, string viewerId)
    {
        return _db.Set<ExamAttemptAnswer>().AsNoTracking()
            .Include(a => a.Attempt)!.ThenInclude(at => at!.Exam)!.ThenInclude(e => e!.Course)
            .FirstOrDefaultAsync(a => a.Id == answerId
                && (a.Attempt!.StudentId == viewerId || a.Attempt.Exam!.Course!.InstructorId == viewerId));
    }

    /// <summary>
    /// Instructor grades a manual (short-answer / file-upload) exam answer,
    /// then the attempt's totals and pass status are recalculated.
    /// </summary>
    public async Task<(bool Ok, string? Error)> GradeAsync(int answerId, int score, string? feedback, string graderId)
    {
        var answer = await _db.Set<ExamAttemptAnswer>()
            .Include(a => a.Question)
            .Include(a => a.Attempt)!.ThenInclude(at => at!.Exam)!.ThenInclude(e => e!.Course)
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

        if (answer.Attempt?.Exam?.Course is null || answer.Attempt.Exam.Course.InstructorId != graderId)
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

    /// <summary>Recomputes an attempt's Score/MaxScore/Percent/Passed from its answers.</summary>
    public async Task RecalculateAsync(int attemptId)
    {
        var attempt = await _db.Set<ExamAttempt>()
            .Include(a => a.Exam)
            .Include(a => a.Answers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt?.Exam is null)
        {
            return;
        }

        var scoredAnswers = attempt.Answers.Select(a => new QuestionScoring.ScoredAnswer(
            Question: a.Question!,
            OptionId: a.AnswerOptionId,
            SelectedOptionIds: a.SelectedOptionIds,
            TextAnswer: a.TextAnswer,
            FileAnswerUrl: a.FileAnswerUrl,
            IsCorrect: a.IsCorrect,
            IsGraded: a.IsGraded,
            GradedScore: a.GradedScore));
        var (score, maxScore) = QuestionScoring.ComputeScores(scoredAnswers);
        attempt.Score = score;
        attempt.MaxScore = maxScore;
        attempt.Percent = maxScore > 0 ? (int)Math.Round(score * 100.0 / maxScore) : 0;
        attempt.Passed = attempt.Percent >= attempt.Exam.PassPercent;
        await _db.SaveChangesAsync();
    }
}
