using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Services;

public record AnswerOptionInput(string Text, bool IsCorrect);

public class QuestionService
{
    private readonly DbContext _db;

    public QuestionService(DbContext db)
    {
        _db = db;
    }

    public Task<Question?> GetByIdAsync(int id)
        => _db.Set<Question>().AsNoTracking()
            .Include(q => q.Quiz).ThenInclude(q => q!.Course)
            .Include(q => q.AnswerOptions.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == id);

    public Task<bool> IsOwnerAsync(int questionId, string userId)
        => _db.Set<Question>().AsNoTracking()
            .AnyAsync(q => q.Id == questionId && q.Quiz!.Course!.InstructorId == userId);

    public async Task<(Question? Question, string? Error)> AddAsync(
        int quizId, string ownerId, string text, int points, List<AnswerOptionInput> options)
    {
        if (!TryValidateOptions(options, out var validationError))
        {
            return (null, validationError);
        }

        var quiz = await _db.Set<Quiz>()
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz?.Course is null || quiz.Course.InstructorId != ownerId)
        {
            return (null, "You do not own this course.");
        }

        var nextOrder = await _db.Set<Question>()
            .Where(q => q.QuizId == quizId)
            .Select(q => (int?)q.OrderIndex)
            .MaxAsync() ?? 0;

        var question = new Question { QuizId = quizId, Text = text, OrderIndex = nextOrder + 1, Points = points };
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
        int questionId, string ownerId, string text, int points, List<AnswerOptionInput> options)
    {
        if (!TryValidateOptions(options, out var validationError))
        {
            return (false, validationError);
        }

        var question = await _db.Set<Question>()
            .Include(q => q.Quiz).ThenInclude(q => q!.Course)
            .Include(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question?.Quiz?.Course is null || question.Quiz.Course.InstructorId != ownerId)
        {
            return (false, "You do not own this course.");
        }

        question.Text = text;
        question.Points = points;
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

    public async Task<bool> DeleteAsync(int questionId, string ownerId)
    {
        var question = await _db.Set<Question>()
            .Include(q => q.Quiz).ThenInclude(q => q!.Course)
            .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question?.Quiz?.Course is null || question.Quiz.Course.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<Question>().Remove(question);
        await _db.SaveChangesAsync();
        return true;
    }

    private static bool TryValidateOptions(List<AnswerOptionInput> options, out string? error)
    {
        if (options.Count is < 2 or > 4)
        {
            error = "A question must have between 2 and 4 answer options.";
            return false;
        }

        if (options.Count(o => o.IsCorrect) != 1)
        {
            error = "Exactly one answer option must be marked correct.";
            return false;
        }

        error = null;
        return true;
    }
}
