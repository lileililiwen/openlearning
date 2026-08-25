using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.QuestionIO.Models;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.QuestionIO.Services;

public enum QuestionImportOutcomeKind
{
    Error,
    RateLimited,
    Submitted,
    Completed,
}

/// <summary>One parsed row as shown in the import page preview.</summary>
public sealed record QuestionRowPreview(int RowIndex, string Stem, string QuestionType, bool Ok, string? Error);

/// <summary>Result of an import submission (sync result, async job id, or rejection).</summary>
public sealed record QuestionImportOutcome(
    QuestionImportOutcomeKind Kind,
    string? Error,
    int? RetryAfterSeconds,
    int? JobId,
    int TotalRows,
    int SuccessCount,
    IReadOnlyList<QuestionRowError> Errors,
    IReadOnlyList<QuestionRowPreview> Preview);

/// <summary>
/// Excel question import. Small files (≤ <c>question.import.syncMaxRows</c>) are
/// imported synchronously with row-by-row errors; larger files are submitted to
/// the async-io pipeline as kind <c>question-import</c>.
/// </summary>
public class QuestionImportService : IAsyncIOProcessor
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int _defaultMaxBytes = 10 * 1024 * 1024;
    private const int _defaultSyncMaxRows = 200;

    private readonly DbContext _db;
    private readonly StorageService _storage;
    private readonly AsyncIOService _asyncIO;
    private readonly QuestionImportRateLimiter _rateLimiter;
    private readonly SystemConfigService _config;

    public QuestionImportService(
        DbContext db,
        StorageService storage,
        AsyncIOService asyncIO,
        QuestionImportRateLimiter rateLimiter,
        SystemConfigService config)
    {
        _db = db;
        _storage = storage;
        _asyncIO = asyncIO;
        _rateLimiter = rateLimiter;
        _config = config;
    }

    public string Kind => "question-import";

    public bool NeedsSourceFile => true;

    /// <summary>
    /// Validates the upload, enforces the per-user rate limit, then either runs
    /// the sync import or submits an async job. <paramref name="forceAsync"/>
    /// routes to async regardless of row count (the ImportAsync page).
    /// </summary>
    public async Task<QuestionImportOutcome> ImportAsync(
        IFormFile? file,
        string ownerId,
        int? quizId,
        QuestionImportMode mode,
        bool isBank,
        bool forceAsync)
    {
        if (file is null || file.Length == 0)
        {
            return Fail("请选择要上传的 .xlsx 文件。");
        }

        var validationError = await ValidateUploadAsync(file);
        if (validationError is not null)
        {
            return Fail(validationError);
        }

        var rate = await _rateLimiter.CheckAsync(ownerId);
        if (!rate.Allowed)
        {
            return new QuestionImportOutcome(
                QuestionImportOutcomeKind.RateLimited,
                "导入过于频繁，请一小时后再试。",
                rate.RetryAfterSeconds,
                null,
                0,
                0,
                Array.Empty<QuestionRowError>(),
                Array.Empty<QuestionRowPreview>());
        }

        if (quizId is not null && !isBank)
        {
            var owned = await _db.Set<Quiz>().AnyAsync(q => q.Id == quizId && q.Course!.InstructorId == ownerId);
            if (!owned)
            {
                return Fail("您不是该测验的所有者，无法导入题目。");
            }
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        List<ParsedQuestionRow> rows;
        try
        {
            rows = ParseRows(stream, isBank);
        }
        catch (InvalidDataException ex)
        {
            return Fail(ex.Message);
        }

        if (rows.Count == 0)
        {
            return Fail("文件中没有数据行。");
        }

        var syncMax = await _config.GetIntAsync("question.import.syncMaxRows", _defaultSyncMaxRows);
        if (forceAsync || rows.Count > syncMax)
        {
            return await SubmitAsync(file, ownerId, quizId, mode, isBank);
        }

        return await ImportSyncCoreAsync(rows, ownerId, quizId, mode, isBank);
    }

    /// <summary>
    /// Async processor invoked by the async-io dispatcher for kind
    /// <c>question-import</c>: parses the stored file, validates, persists
    /// correct rows, writes the error file, and mirrors the outcome.
    /// </summary>
    public async Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream? fileStream, CancellationToken cancellationToken)
    {
        var meta = await _db.Set<QuestionImportJob>().FirstOrDefaultAsync(j => j.AsyncIOJobId == job.Id, cancellationToken);
        if (meta is null)
        {
            return (false, "未找到导入任务元数据。", 0, 0);
        }

        if (fileStream is null)
        {
            return (false, "源文件缺失。", 0, 0);
        }

        meta.Status = QuestionImportJobStatus.Running;
        meta.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = ParseRows(fileStream, meta.IsBank);
            var errors = new List<QuestionRowError>();
            var success = 0;
            var nextOrder = await NextOrderAsync(meta.QuizId, meta.IsBank);

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (rowErrors, input) = ValidateRow(row);
                if (rowErrors.Count > 0)
                {
                    errors.AddRange(rowErrors);
                    continue;
                }

                var result = await PersistRowAsync(row, input!, meta.UserId, meta.QuizId, meta.Mode, meta.IsBank);
                if (result.Error is not null)
                {
                    errors.Add(new QuestionRowError { RowIndex = row.RowIndex, Field = result.Field, Message = result.Error });
                }
                else
                {
                    success++;
                    nextOrder++;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            meta.TotalRows = rows.Count;
            meta.SuccessRows = success;
            meta.ErrorRows = errors.Count;
            meta.Status = QuestionImportJobStatus.Success;
            meta.FinishedAt = DateTime.UtcNow;

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    error.JobId = meta.Id;
                    _db.Set<QuestionRowError>().Add(error);
                }

                await _db.SaveChangesAsync(cancellationToken);
                var errorKey = await WriteErrorFileAsync(job.Id, meta.UserId, errors);
                meta.ErrorFileKey = errorKey;
                job.ErrorFileKey = errorKey;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return (true, null, rows.Count, success);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            meta.Status = QuestionImportJobStatus.Failed;
            meta.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return (false, ex.Message, 0, 0);
        }
    }

    private async Task<QuestionImportOutcome> SubmitAsync(
        IFormFile file, string ownerId, int? quizId, QuestionImportMode mode, bool isBank)
    {
        var (job, error) = await _asyncIO.SubmitAsync(ownerId, Kind, new XlsxFileValidator(), file);
        if (error is not null || job is null)
        {
            return Fail(error ?? "提交导入任务失败。");
        }

        var importJob = new QuestionImportJob
        {
            UserId = ownerId,
            QuizId = isBank ? null : quizId,
            IsBank = isBank,
            Mode = mode,
            FileKey = job.FileKey,
            AsyncIOJobId = job.Id,
        };
        _db.Set<QuestionImportJob>().Add(importJob);
        await _db.SaveChangesAsync();

        return new QuestionImportOutcome(
            QuestionImportOutcomeKind.Submitted,
            null,
            null,
            job.Id,
            0,
            0,
            Array.Empty<QuestionRowError>(),
            Array.Empty<QuestionRowPreview>());
    }

    private async Task<QuestionImportOutcome> ImportSyncCoreAsync(
        List<ParsedQuestionRow> rows,
        string ownerId,
        int? quizId,
        QuestionImportMode mode,
        bool isBank)
    {
        var errors = new List<QuestionRowError>();
        var previews = new List<QuestionRowPreview>();
        var success = 0;
        var nextOrder = await NextOrderAsync(quizId, isBank);

        foreach (var row in rows)
        {
            var (rowErrors, input) = ValidateRow(row);
            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors);
                previews.Add(new QuestionRowPreview(row.RowIndex, row.Stem ?? string.Empty, row.QuestionTypeText ?? string.Empty, false, rowErrors[0].Message));
                continue;
            }

            var result = await PersistRowAsync(row, input!, ownerId, quizId, mode, isBank);
            if (result.Error is not null)
            {
                errors.Add(new QuestionRowError { RowIndex = row.RowIndex, Field = result.Field, Message = result.Error });
                previews.Add(new QuestionRowPreview(row.RowIndex, row.Stem ?? string.Empty, row.QuestionTypeText ?? string.Empty, false, result.Error));
            }
            else
            {
                success++;
                nextOrder++;
                previews.Add(new QuestionRowPreview(row.RowIndex, row.Stem ?? string.Empty, row.QuestionTypeText ?? string.Empty, true, null));
            }
        }

        await _db.SaveChangesAsync();

        return new QuestionImportOutcome(
            QuestionImportOutcomeKind.Completed,
            null,
            null,
            null,
            rows.Count,
            success,
            errors,
            previews);
    }

    private async Task<int> NextOrderAsync(int? quizId, bool isBank)
    {
        if (isBank)
        {
            return 0;
        }

        return (await _db.Set<Question>()
                .Where(q => q.QuizId == quizId)
                .Select(q => (int?)q.OrderIndex)
                .MaxAsync() ?? 0) + 1;
    }

    /// <summary>Persists one validated row (create or, in UpdateOrAppend, update). Does not save.</summary>
    private async Task<(string? Error, string Field)> PersistRowAsync(
        ParsedQuestionRow row,
        QuestionRowInput input,
        string ownerId,
        int? quizId,
        QuestionImportMode mode,
        bool isBank)
    {
        if (mode == QuestionImportMode.UpdateOrAppend && input.RowId is not null)
        {
            if (isBank)
            {
                var existing = await _db.Set<Question>().Include(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.IsBank && q.RowId == input.RowId);
                if (existing is not null)
                {
                    ApplyUpdate(existing, input, row.BankTopic);
                    return (null, string.Empty);
                }
            }
            else
            {
                var foreign = await _db.Set<Question>().AsNoTracking().AnyAsync(
                    q => q.RowId == input.RowId
                        && q.Quiz != null
                        && q.Quiz.Course != null
                        && q.Quiz.Course.InstructorId != ownerId);
                if (foreign)
                {
                    return ("not owner", "RowId");
                }

                var existing = await _db.Set<Question>().Include(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId && q.RowId == input.RowId);
                if (existing is not null)
                {
                    ApplyUpdate(existing, input, null);
                    return (null, string.Empty);
                }
            }
        }

        if (input.RowId is not null)
        {
            var duplicate = isBank
                ? await _db.Set<Question>().AnyAsync(q => q.IsBank && q.RowId == input.RowId)
                : await _db.Set<Question>().AnyAsync(q => q.QuizId == quizId && q.RowId == input.RowId);
            if (duplicate)
            {
                return ("duplicate row id", "RowId");
            }
        }

        var question = new Question
        {
            QuizId = isBank ? null : quizId,
            IsBank = isBank,
            BankTopic = isBank && !string.IsNullOrWhiteSpace(row.BankTopic)
                ? row.BankTopic.Trim()
                : null,
            Text = input.Stem,
            QuestionType = input.QuestionType,
            Points = 1,
            RowId = input.RowId,
            Difficulty = input.Difficulty,
            KnowledgeTag = input.KnowledgeTag,
            Explanation = input.Explanation,
        };
        for (var i = 0; i < input.Options.Count; i++)
        {
            question.AnswerOptions.Add(new AnswerOption
            {
                Text = input.Options[i].Text,
                IsCorrect = input.Options[i].IsCorrect,
                OrderIndex = i + 1,
            });
        }

        _db.Set<Question>().Add(question);
        return (null, string.Empty);
    }

    private void ApplyUpdate(Question existing, QuestionRowInput input, string? bankTopic)
    {
        existing.Text = input.Stem;
        existing.QuestionType = input.QuestionType;
        existing.RowId = input.RowId;
        existing.Difficulty = input.Difficulty;
        existing.KnowledgeTag = input.KnowledgeTag;
        existing.Explanation = input.Explanation;
        if (existing.IsBank && !string.IsNullOrWhiteSpace(bankTopic))
        {
            existing.BankTopic = bankTopic.Trim();
        }

        if (existing.AnswerOptions.Count > 0)
        {
            _db.Set<AnswerOption>().RemoveRange(existing.AnswerOptions);
            existing.AnswerOptions.Clear();
        }

        for (var i = 0; i < input.Options.Count; i++)
        {
            existing.AnswerOptions.Add(new AnswerOption
            {
                Text = input.Options[i].Text,
                IsCorrect = input.Options[i].IsCorrect,
                OrderIndex = i + 1,
            });
        }
    }

    private static (List<QuestionRowError> Errors, QuestionRowInput? Input) ValidateRow(ParsedQuestionRow row)
    {
        var errors = new List<QuestionRowError>();
        var stem = row.Stem?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(stem))
        {
            errors.Add(RowError(row, "Stem", "题干（Stem）为必填项。"));
        }

        var typeText = row.QuestionTypeText?.Trim() ?? string.Empty;
        if (!TryParseQuestionType(typeText, out var type))
        {
            errors.Add(RowError(row, "QuestionType", "不支持的题型，允许的值：SingleChoice, MultipleChoice, TrueFalse, FillBlank, ShortAnswer, FileUpload。"));
        }

        var correctAnswer = row.CorrectAnswer?.Trim();
        if (type is not null && type is not (QuestionType.ShortAnswer or QuestionType.FileUpload) && string.IsNullOrWhiteSpace(correctAnswer))
        {
            errors.Add(RowError(row, "CorrectAnswer", "客观题必须填写正确答案。"));
        }

        var difficultyText = row.DifficultyText?.Trim();
        if (!string.IsNullOrWhiteSpace(difficultyText) && !TryParseDifficulty(difficultyText, out _))
        {
            errors.Add(RowError(row, "Difficulty", "难度必须是 Easy、Medium 或 Hard（不区分大小写）。"));
        }

        var knowledgeTag = row.KnowledgeTag?.Trim();
        if (knowledgeTag is { Length: > 200 })
        {
            errors.Add(RowError(row, "KnowledgeTag", "知识点标签不能超过 200 个字符。"));
        }

        var rowId = row.RowId?.Trim();
        if (rowId is { Length: > 100 })
        {
            errors.Add(RowError(row, "RowId", "RowId 不能超过 100 个字符。"));
        }

        var options = new List<AnswerOptionInput>();
        switch (type)
        {
            case QuestionType.SingleChoice or QuestionType.MultipleChoice:
                {
                    var optionTexts = new[] { row.OptionA, row.OptionB, row.OptionC, row.OptionD };
                    if (optionTexts.Count(o => !string.IsNullOrWhiteSpace(o?.Trim())) < 2)
                    {
                        errors.Add(RowError(row, "OptionA", "单选/多选题至少需要填写 2 个选项（A-D）。"));
                    }

                    if (!TryParseCorrectLetters(correctAnswer, out var letters))
                    {
                        errors.Add(RowError(row, "CorrectAnswer", "正确答案必须是选项字母（如 A 或 A,C）。"));
                    }
                    else
                    {
                        foreach (var letter in letters)
                        {
                            var index = letter - 'A';
                            if (string.IsNullOrWhiteSpace(optionTexts[index]?.Trim()))
                            {
                                errors.Add(RowError(row, "CorrectAnswer", $"选项 {letter} 未填写，不能标记为正确答案。"));
                            }
                        }

                        for (var i = 0; i < 4; i++)
                        {
                            var text = optionTexts[i]?.Trim();
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                continue;
                            }

                            options.Add(new AnswerOptionInput(text, letters.Contains((char)('A' + i))));
                        }

                        if (type == QuestionType.SingleChoice && options.Count(o => o.IsCorrect) != 1)
                        {
                            errors.Add(RowError(row, "CorrectAnswer", "单选题必须恰好有一个正确答案。"));
                        }

                        if (type == QuestionType.MultipleChoice && !options.Any(o => o.IsCorrect))
                        {
                            errors.Add(RowError(row, "CorrectAnswer", "多选题至少需要一个正确答案。"));
                        }
                    }

                    break;
                }

            case QuestionType.TrueFalse:
                {
                    if (!TryParseTrueFalse(correctAnswer, out var value))
                    {
                        errors.Add(RowError(row, "CorrectAnswer", "判断题的正确答案必须是 True 或 False。"));
                    }
                    else
                    {
                        options.Add(new AnswerOptionInput("True", value));
                        options.Add(new AnswerOptionInput("False", !value));
                    }

                    break;
                }

            case QuestionType.FillBlank:
                {
                    var answers = SplitFillBlankAnswers(correctAnswer);
                    if (answers.Count is < 1 or > 4)
                    {
                        errors.Add(RowError(row, "CorrectAnswer", "填空题需提供 1-4 个可接受答案（用 | 分隔）。"));
                    }
                    else
                    {
                        options.AddRange(answers.Select(answer => new AnswerOptionInput(answer, true)));
                    }

                    break;
                }
        }

        if (errors.Count > 0)
        {
            return (errors, null);
        }

        var difficulty = string.IsNullOrWhiteSpace(difficultyText) ? QuestionDifficulty.Easy : ParseDifficulty(difficultyText);
        var explanation = row.Explanation?.Trim();
        var tag = knowledgeTag is { Length: > 0 } ? knowledgeTag : null;
        var input = new QuestionRowInput(rowId, stem, type!.Value, options, explanation is { Length: > 0 } ? explanation : null, difficulty, tag);
        return (errors, input);
    }

    private static QuestionRowError RowError(ParsedQuestionRow row, string field, string message)
    {
        return new QuestionRowError { RowIndex = row.RowIndex, Field = field, Message = message };
    }

    private static QuestionImportOutcome Fail(string message)
    {
        return new QuestionImportOutcome(
            QuestionImportOutcomeKind.Error,
            message,
            null,
            null,
            0,
            0,
            Array.Empty<QuestionRowError>(),
            Array.Empty<QuestionRowPreview>());
    }

    private async Task<string?> ValidateUploadAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            return "仅支持 .xlsx 文件。";
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) && !file.ContentType.Equals(_contentTypeXlsx, StringComparison.OrdinalIgnoreCase))
        {
            return "仅支持 .xlsx 文件。";
        }

        var maxBytes = await _config.GetIntAsync("question.import.maxBytes", _defaultMaxBytes);
        if (file.Length > maxBytes)
        {
            return $"文件超过大小限制（{maxBytes / (1024 * 1024)} MB）。";
        }

        return null;
    }

    private async Task<string?> WriteErrorFileAsync(int asyncIOJobId, string ownerId, List<QuestionRowError> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Errors");
            sheet.Cell(1, 1).Value = "RowIndex";
            sheet.Cell(1, 2).Value = "Field";
            sheet.Cell(1, 3).Value = "Message";
            for (var i = 0; i < errors.Count; i++)
            {
                sheet.Cell(i + 2, 1).Value = errors[i].RowIndex;
                sheet.Cell(i + 2, 2).Value = errors[i].Field;
                sheet.Cell(i + 2, 3).Value = errors[i].Message;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var (file, error) = await _storage.UploadAsync(ownerId, FilePurpose.AsyncIO, $"errors-{asyncIOJobId}.xlsx", _contentTypeXlsx, stream);
        return error is null && file is not null ? file.Key : null;
    }

    private static List<ParsedQuestionRow> ParseRows(Stream stream, bool includeBankTopic)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidDataException("工作簿中没有工作表。");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastHeaderColumn = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var c = 1; c <= lastHeaderColumn; c++)
        {
            var name = sheet.Cell(1, c).GetString().Trim();
            if (name.Length > 0 && !headers.ContainsKey(name))
            {
                headers[name] = c;
            }
        }

        if (!headers.ContainsKey("QuestionType") || !headers.ContainsKey("Stem"))
        {
            throw new InvalidDataException("文件缺少必需列（QuestionType, Stem）。");
        }

        var rows = new List<ParsedQuestionRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var values = headers.ToDictionary(h => h.Key, h => CellText(sheet.Cell(r, h.Value)), StringComparer.OrdinalIgnoreCase);
            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ParsedQuestionRow(
                r,
                values.GetValueOrDefault("RowId"),
                values.GetValueOrDefault("QuestionType"),
                values.GetValueOrDefault("Stem"),
                values.GetValueOrDefault("OptionA"),
                values.GetValueOrDefault("OptionB"),
                values.GetValueOrDefault("OptionC"),
                values.GetValueOrDefault("OptionD"),
                values.GetValueOrDefault("CorrectAnswer"),
                values.GetValueOrDefault("Explanation"),
                values.GetValueOrDefault("Difficulty"),
                values.GetValueOrDefault("KnowledgeTag"),
                includeBankTopic ? values.GetValueOrDefault("BankTopic") : null));
        }

        return rows;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        var value = cell.Value;
        return value.Type switch
        {
            XLDataType.Number => value.GetNumber().ToString("0.########", CultureInfo.InvariantCulture),
            XLDataType.Boolean => value.GetBoolean() ? "True" : "False",
            XLDataType.DateTime => value.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            _ => value.GetText(),
        };
    }

    private static bool IsSupportedQuestionType(QuestionType type)
    {
        return type is >= QuestionType.SingleChoice and <= QuestionType.FileUpload;
    }

    private static bool TryParseQuestionType(string? text, out QuestionType? type)
    {
        type = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (Enum.TryParse(text.Trim(), ignoreCase: true, out QuestionType parsed) && IsSupportedQuestionType(parsed))
        {
            type = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseDifficulty(string? text, out QuestionDifficulty difficulty)
    {
        return Enum.TryParse(text?.Trim(), ignoreCase: true, out difficulty);
    }

    private static QuestionDifficulty ParseDifficulty(string text)
    {
        Enum.TryParse(text.Trim(), ignoreCase: true, out QuestionDifficulty difficulty);
        return difficulty;
    }

    private static bool TryParseCorrectLetters(string? text, out List<char> letters)
    {
        letters = new List<char>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        char[] separators = [',', ';'];
        foreach (var part in text.Split(separators, StringSplitOptions.None))
        {
            var token = part.Trim().ToUpperInvariant();
            if (token.Length != 1 || token[0] is < 'A' or > 'D')
            {
                return false;
            }

            letters.Add(token[0]);
        }

        return letters.Count > 0;
    }

    private static bool TryParseTrueFalse(string? text, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (text.Trim().Equals("False", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static List<string> SplitFillBlankAnswers(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// <summary>Allows the async-io submit path to re-validate cheaply (size was already enforced).</summary>
    private sealed class XlsxFileValidator : IIOFileValidator
    {
        public string[] AllowedExtensions { get; } = [".xlsx"];

        public long MaxBytes => long.MaxValue;

        public string? Validate(IFormFile file)
        {
            return string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)
                ? null
                : "仅支持 .xlsx 文件。";
        }
    }
}
