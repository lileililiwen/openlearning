using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Services;

/// <summary>
/// Persistent per-student incorrect-answer log with bookmarking and a
/// practice-question builder. Quiz and exam scoring hook into
/// <see cref="RecordAsync"/>; answering correctly in practice resolves entries.
/// </summary>
public class IncorrectAnswerService
{
    private readonly DbContext _db;

    public IncorrectAnswerService(DbContext db)
    {
        _db = db;
    }

    /// <summary>Records one wrong answer; never duplicates an active entry for the same source.</summary>
    public async Task RecordAsync(
        string userId, Question question, int courseId, string chosenAnswer, string correctAnswer,
        IncorrectAnswerSource sourceType, int sourceId)
    {
        if (await _db.Set<IncorrectAnswer>().AnyAsync(x =>
                x.UserId == userId && x.QuestionId == question.Id &&
                x.SourceType == sourceType && x.SourceId == sourceId && x.ResolvedAt == null))
        {
            return;
        }

        _db.Set<IncorrectAnswer>().Add(new IncorrectAnswer
        {
            UserId = userId,
            QuestionId = question.Id,
            CourseId = courseId,
            ChosenAnswer = chosenAnswer,
            CorrectAnswer = correctAnswer,
            SourceType = sourceType,
            SourceId = sourceId,
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<IncorrectAnswer>> ListAsync(string userId, bool unresolvedOnly, bool bookmarkedOnly)
    {
        var query = _db.Set<IncorrectAnswer>().AsNoTracking()
            .Include(x => x.Question)!.ThenInclude(q => q!.AnswerOptions)
            .Where(x => x.UserId == userId);
        if (unresolvedOnly)
        {
            query = query.Where(x => x.ResolvedAt == null);
        }

        if (bookmarkedOnly)
        {
            query = query.Where(x => _db.Set<BookmarkedQuestion>()
                .Any(b => b.UserId == userId && b.QuestionId == x.QuestionId));
        }

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public Task<int> GetActiveCountAsync(string userId)
    {
        return _db.Set<IncorrectAnswer>()
            .CountAsync(x => x.UserId == userId && x.ResolvedAt == null);
    }

    public Task<bool> IsBookmarkedAsync(string userId, int questionId)
    {
        return _db.Set<BookmarkedQuestion>()
            .AnyAsync(b => b.UserId == userId && b.QuestionId == questionId);
    }

    public async Task<HashSet<int>> GetBookmarkedIdsAsync(string userId, IEnumerable<int> questionIds)
    {
        var ids = questionIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var bookmarked = await _db.Set<BookmarkedQuestion>()
            .Where(b => b.UserId == userId && ids.Contains(b.QuestionId))
            .Select(b => b.QuestionId)
            .ToListAsync();
        return bookmarked.ToHashSet();
    }

    public async Task ToggleBookmarkAsync(string userId, int questionId)
    {
        var existing = await _db.Set<BookmarkedQuestion>()
            .FirstOrDefaultAsync(b => b.UserId == userId && b.QuestionId == questionId);
        if (existing is null)
        {
            _db.Set<BookmarkedQuestion>().Add(new BookmarkedQuestion { UserId = userId, QuestionId = questionId });
        }
        else
        {
            _db.Set<BookmarkedQuestion>().Remove(existing);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>Marks all active log entries for the question resolved (practiced correctly).</summary>
    public async Task ResolveAsync(string userId, int questionId)
    {
        var entries = await _db.Set<IncorrectAnswer>()
            .Where(x => x.UserId == userId && x.QuestionId == questionId && x.ResolvedAt == null)
            .ToListAsync();
        foreach (var entry in entries)
        {
            entry.ResolvedAt = DateTime.UtcNow;
        }

        if (entries.Count > 0)
        {
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Distinct objective questions from the active log, for a practice quiz.</summary>
    public async Task<List<Question>> BuildPracticeQuestionsAsync(string userId)
    {
        var questionIds = await _db.Set<IncorrectAnswer>()
            .Where(x => x.UserId == userId && x.ResolvedAt == null)
            .Select(x => x.QuestionId)
            .Distinct()
            .ToListAsync();
        if (questionIds.Count == 0)
        {
            return new List<Question>();
        }

        return await _db.Set<Question>().AsNoTracking()
            .Include(q => q.AnswerOptions)
            .Where(q => questionIds.Contains(q.Id)
                && q.QuestionType != QuestionType.ShortAnswer
                && q.QuestionType != QuestionType.FileUpload)
            .OrderBy(q => q.Id)
            .ToListAsync();
    }

    /// <summary>Human-readable chosen/correct answer text for the log.</summary>
    public static (string Chosen, string Correct) FormatAnswer(
        Question question, int? optionId, string? selectedOptionIds, string? textAnswer, string? fileAnswerUrl)
    {
        switch (question.QuestionType)
        {
            case QuestionType.SingleChoice:
            case QuestionType.TrueFalse:
                var selected = question.AnswerOptions.FirstOrDefault(o => o.Id == optionId);
                var correct = question.AnswerOptions.FirstOrDefault(o => o.IsCorrect);
                return (selected?.Text ?? "未作答", correct?.Text ?? string.Empty);
            case QuestionType.MultipleChoice:
                var selectedIds = ParseIds(selectedOptionIds);
                var chosen = string.Join(", ", question.AnswerOptions
                    .Where(o => selectedIds.Contains(o.Id)).Select(o => o.Text));
                var correctTexts = string.Join(", ", question.AnswerOptions
                    .Where(o => o.IsCorrect).Select(o => o.Text));
                return (chosen.Length == 0 ? "未作答" : chosen, correctTexts);
            case QuestionType.FillBlank:
                return (textAnswer ?? "未作答",
                    string.Join(" / ", question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Text)));
            case QuestionType.FileUpload:
                return (string.IsNullOrEmpty(fileAnswerUrl) ? "未作答" : "已上传文件", string.Empty);
            default:
                return (textAnswer ?? "未作答", string.Empty);
        }
    }

    private static HashSet<int> ParseIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<int>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .ToHashSet();
    }
}
