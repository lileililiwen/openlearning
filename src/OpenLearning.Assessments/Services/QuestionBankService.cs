using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Assessments.Services;

/// <summary>
/// Central question bank. Bank rows are ordinary <see cref="Question"/> rows
/// with IsBank=true and no quiz/exam association. Importing copies the row
/// (including options) into a quiz or exam as a snapshot — later bank edits
/// never change in-use copies.
/// </summary>
public class QuestionBankService
{
    private readonly DbContext _db;

    public QuestionBankService(DbContext db)
    {
        _db = db;
    }

    public Task<Question?> GetByIdAsync(int id)
    {
        return _db.Set<Question>().AsNoTracking()
                .Include(q => q.AnswerOptions.OrderBy(o => o.OrderIndex))
                .FirstOrDefaultAsync(q => q.Id == id && q.IsBank);
    }

    public async Task<(Question? Question, string? Error)> CreateAsync(
        string text, int points, QuestionType questionType, string? topic, List<AnswerOptionInput> options)
    {
        if (!QuestionService.TryValidateOptions(questionType, options, out var validationError))
        {
            return (null, validationError);
        }

        var question = new Question
        {
            IsBank = true,
            BankTopic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim(),
            Text = text,
            QuestionType = questionType,
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

    public async Task<(bool Ok, string? Error)> UpdateAsync(
        int questionId, string text, int points, QuestionType questionType, string? topic, List<AnswerOptionInput> options)
    {
        if (!QuestionService.TryValidateOptions(questionType, options, out var validationError))
        {
            return (false, validationError);
        }

        var question = await _db.Set<Question>()
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == questionId && q.IsBank);
        if (question is null)
        {
            return (false, "Bank question not found.");
        }

        question.Text = text;
        question.Points = points;
        question.QuestionType = questionType;
        question.BankTopic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
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

    public async Task<bool> ArchiveAsync(int questionId)
    {
        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId && q.IsBank);
        if (question is null)
        {
            return false;
        }

        question.ArchivedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Searchable, paginated list of active bank questions.</summary>
    public async Task<(List<Question> Items, int Total)> SearchAsync(string? topic, string? text, int page = 1, int pageSize = 20)
    {
        var query = _db.Set<Question>().AsNoTracking()
            .Where(q => q.IsBank && q.ArchivedAt == null);

        if (!string.IsNullOrWhiteSpace(topic))
        {
            query = query.Where(q => q.BankTopic != null && q.BankTopic.Contains(topic));
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(q => q.Text.Contains(text));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(q => q.AnswerOptions)
            .ToListAsync();
        return (items, total);
    }

    /// <summary>Copies a bank question into a quiz (snapshot); the quiz must be owned by the caller.</summary>
    public async Task<(bool Ok, string? Error)> ImportIntoQuizAsync(int bankQuestionId, int quizId, string ownerId)
    {
        var quiz = await _db.Set<Quiz>()
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz?.Course is null || quiz.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        var source = await _db.Set<Question>()
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == bankQuestionId && q.IsBank && q.ArchivedAt == null);
        if (source is null)
        {
            return (false, "Bank question not found.");
        }

        var nextOrder = await _db.Set<Question>()
            .Where(q => q.QuizId == quizId)
            .Select(q => (int?)q.OrderIndex)
            .MaxAsync() ?? 0;

        var copy = new Question
        {
            QuizId = quizId,
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

    public Task<bool> IsCourseOwnerAsync(int courseId, string userId)
    {
        return _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }
}
