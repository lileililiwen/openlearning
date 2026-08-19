using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;

namespace OpenLearning.QuestionIO.Services;

public sealed record QuestionExportFilters(
    int? QuizId,
    QuestionType? Type,
    QuestionDifficulty? Difficulty,
    string? KnowledgeTag,
    bool IsBank,
    string? BankTopic);

/// <summary>
/// Excel export of questions. Runs synchronously up to
/// <c>question.export.syncMaxRows</c> (5000); larger exports go through the
/// async-io pipeline as kind <c>question-export</c>, which streams the workbook
/// and delivers an <c>export.ready</c> notification.
/// </summary>
public class QuestionExportService : IAsyncIOProcessor
{
    private readonly DbContext _db;
    private readonly AsyncIOService _asyncIO;

    public QuestionExportService(DbContext db, AsyncIOService asyncIO)
    {
        _db = db;
        _asyncIO = asyncIO;
    }

    public string Kind => "question-export";

    public bool NeedsSourceFile => false;

    public async Task<int> CountAsync(QuestionExportFilters filters, string ownerId, bool isAdmin)
    {
        var query = await BuildQueryAsync(filters, ownerId, isAdmin);
        return query is null ? 0 : await query.CountAsync();
    }

    /// <summary>Builds the workbook synchronously and returns its bytes (streamed in chunks).</summary>
    public async Task<(byte[]? Bytes, string? Error, int RowCount)> ExportSyncAsync(
        QuestionExportFilters filters, string ownerId, bool isAdmin)
    {
        var query = await BuildQueryAsync(filters, ownerId, isAdmin);
        if (query is null)
        {
            return (null, "您不是该测验的所有者，无法导出题目。", 0);
        }

        using var stream = new MemoryStream();
        var options = await LoadOptionsAsync(filters);
        var rowCount = await WriteWorkbookAsync(stream, query, options);
        stream.Position = 0;
        return (stream.ToArray(), null, rowCount);
    }

    public async Task<(int? JobId, string? Error)> SubmitExportAsync(
        QuestionExportFilters filters, string ownerId, bool isAdmin)
    {
        var payload = new ExportPayload(
            filters.QuizId,
            filters.Type?.ToString(),
            filters.Difficulty?.ToString(),
            filters.KnowledgeTag,
            filters.IsBank,
            filters.BankTopic,
            isAdmin);
        var job = new AsyncIOJob
        {
            UserId = ownerId,
            Kind = Kind,
            FileKey = string.Empty,
            Payload = JsonSerializer.Serialize(payload),
        };
        _db.Set<AsyncIOJob>().Add(job);
        await _db.SaveChangesAsync();
        return (job.Id, null);
    }

    /// <summary>Async processor for kind <c>question-export</c>: rebuilds filters from the payload and streams the workbook.</summary>
    public async Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream? fileStream, CancellationToken cancellationToken)
    {
        ExportPayload? payload;
        try
        {
            payload = string.IsNullOrWhiteSpace(job.Payload) ? null : JsonSerializer.Deserialize<ExportPayload>(job.Payload);
        }
        catch (JsonException)
        {
            payload = null;
        }

        if (payload is null)
        {
            return (false, "导出参数缺失。", 0, 0);
        }

        var filters = new QuestionExportFilters(
            payload.QuizId,
            ParseType(payload.Type),
            ParseDifficulty(payload.Difficulty),
            payload.KnowledgeTag,
            payload.IsBank,
            payload.BankTopic);
        var query = await BuildQueryAsync(filters, job.UserId, payload.IsAdmin);
        if (query is null)
        {
            return (false, "您不是该测验的所有者，无法导出题目。", 0, 0);
        }

        using var stream = new MemoryStream();
        var options = await LoadOptionsAsync(filters);
        var rowCount = await WriteWorkbookAsync(stream, query, options, cancellationToken);
        stream.Position = 0;
        await _asyncIO.SetResultAsync(job.Id, $"questions-{job.Id}.xlsx", stream);
        return (true, null, rowCount, rowCount);
    }

    private async Task<IQueryable<Question>?> BuildQueryAsync(QuestionExportFilters filters, string ownerId, bool isAdmin)
    {
        IQueryable<Question> query = _db.Set<Question>().AsNoTracking();
        if (filters.IsBank)
        {
            if (!isAdmin)
            {
                return null;
            }

            query = query.Where(q => q.IsBank && q.ArchivedAt == null);
            if (!string.IsNullOrWhiteSpace(filters.BankTopic))
            {
                query = query.Where(q => q.BankTopic != null && q.BankTopic.Contains(filters.BankTopic));
            }
        }
        else
        {
            if (filters.QuizId is not int quizId)
            {
                return null;
            }

            var owned = await _db.Set<Quiz>().AnyAsync(q => q.Id == quizId && q.Course!.InstructorId == ownerId);
            if (!owned)
            {
                return null;
            }

            query = query.Where(q => q.QuizId == quizId);
        }

        if (filters.Type is not null)
        {
            query = query.Where(q => q.QuestionType == filters.Type);
        }

        if (filters.Difficulty is not null)
        {
            query = query.Where(q => q.Difficulty == filters.Difficulty);
        }

        if (!string.IsNullOrWhiteSpace(filters.KnowledgeTag))
        {
            query = query.Where(q => q.KnowledgeTag != null && q.KnowledgeTag.Contains(filters.KnowledgeTag));
        }

        return query.OrderBy(q => q.Id);
    }

    /// <summary>
    /// Loads all answer options for the export scope once, so the workbook is
    /// written without issuing concurrent queries on the same DbContext
    /// (Npgsql does not allow a second command while a query is streaming).
    /// </summary>
    private async Task<Dictionary<int, List<AnswerOption>>> LoadOptionsAsync(QuestionExportFilters filters)
    {
        IQueryable<AnswerOption> query = _db.Set<AnswerOption>().AsNoTracking();
        if (filters.IsBank)
        {
            query = query.Where(o => _db.Set<Question>().Any(q => q.IsBank && q.Id == o.QuestionId));
        }
        else if (filters.QuizId is int quizId)
        {
            query = query.Where(o => _db.Set<Question>().Any(q => q.QuizId == quizId && q.Id == o.QuestionId));
        }

        var options = await query
            .OrderBy(o => o.QuestionId)
            .ThenBy(o => o.OrderIndex)
            .ToListAsync();
        return options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());
    }

    private static async Task<int> WriteWorkbookAsync(
        Stream target,
        IQueryable<Question> query,
        Dictionary<int, List<AnswerOption>> optionsByQuestion,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Questions");
        WriteHeader(sheet);

        var row = 2;
        await foreach (var question in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var opts = optionsByQuestion.GetValueOrDefault(question.Id) ?? new List<AnswerOption>();
            WriteRow(sheet, row, question, opts);
            row++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(target);
        return row - 2;
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        var headers = new[]
        {
            "RowId", "QuestionType", "Stem", "OptionA", "OptionB", "OptionC", "OptionD",
            "CorrectAnswer", "Explanation", "Difficulty", "KnowledgeTag", "BankTopic",
        };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }
    }

    private static void WriteRow(IXLWorksheet sheet, int row, Question question, List<AnswerOption> opts)
    {
        sheet.Cell(row, 1).Value = question.RowId ?? string.Empty;
        sheet.Cell(row, 2).Value = question.QuestionType.ToString();
        sheet.Cell(row, 3).Value = question.Text;
        sheet.Cell(row, 4).Value = opts.Count > 0 ? opts[0].Text : string.Empty;
        sheet.Cell(row, 5).Value = opts.Count > 1 ? opts[1].Text : string.Empty;
        sheet.Cell(row, 6).Value = opts.Count > 2 ? opts[2].Text : string.Empty;
        sheet.Cell(row, 7).Value = opts.Count > 3 ? opts[3].Text : string.Empty;
        sheet.Cell(row, 8).Value = RenderCorrectAnswer(question, opts);
        sheet.Cell(row, 9).Value = question.Explanation ?? string.Empty;
        sheet.Cell(row, 10).Value = question.Difficulty.ToString();
        sheet.Cell(row, 11).Value = question.KnowledgeTag ?? string.Empty;
        sheet.Cell(row, 12).Value = question.BankTopic ?? string.Empty;
    }

    private static string RenderCorrectAnswer(Question question, List<AnswerOption> options)
    {
        switch (question.QuestionType)
        {
            case QuestionType.SingleChoice or QuestionType.MultipleChoice:
                {
                    var letters = options
                        .Select((option, index) => new { option, index })
                        .Where(x => x.option.IsCorrect)
                        .Select(x => ((char)('A' + x.index)).ToString())
                        .ToList();
                    return string.Join(",", letters);
                }

            case QuestionType.TrueFalse:
                {
                    var correct = options.FirstOrDefault(o => o.IsCorrect)?.Text ?? "True";
                    return correct.Equals("False", StringComparison.OrdinalIgnoreCase) ? "False" : "True";
                }

            case QuestionType.FillBlank:
                return string.Join("|", options.Where(o => o.IsCorrect).Select(o => o.Text));

            default:
                return string.Empty;
        }
    }

    private static QuestionType? ParseType(string? text)
    {
        return Enum.TryParse(text, ignoreCase: true, out QuestionType type) ? type : null;
    }

    private static QuestionDifficulty? ParseDifficulty(string? text)
    {
        return Enum.TryParse(text, ignoreCase: true, out QuestionDifficulty difficulty) ? difficulty : null;
    }

    private sealed record ExportPayload(
        int? QuizId,
        string? Type,
        string? Difficulty,
        string? KnowledgeTag,
        bool IsBank,
        string? BankTopic,
        bool IsAdmin);
}
