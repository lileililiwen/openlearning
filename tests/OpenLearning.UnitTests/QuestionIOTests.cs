using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.Assessments.Models;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.QuestionIO.Models;
using OpenLearning.QuestionIO.Services;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;
using Xunit;

namespace OpenLearning.UnitTests.QuestionIO;

public sealed class QuestionIOTests
{
    private const string _xlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] _headers =
    [
        "RowId", "QuestionType", "Stem", "OptionA", "OptionB", "OptionC", "OptionD",
        "CorrectAnswer", "Explanation", "Difficulty", "KnowledgeTag",
    ];

    private static (ApplicationDbContext Db, QuestionImportService Import, QuestionExportService Export, string TempDir) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ol-questionio-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        var asyncIO = new AsyncIOService(db, storage, TestNotificationService.Create(db));
        var config = new SystemConfigService(db);
        var import = new QuestionImportService(db, storage, asyncIO, new QuestionImportRateLimiter(config), config);
        var export = new QuestionExportService(db, asyncIO);
        return (db, import, export, tempDir);
    }

    private static byte[] BuildXlsx(bool includeBankTopic, params string[][] rows)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Questions");
            var headers = includeBankTopic ? _headers.Append("BankTopic").ToArray() : _headers;
            for (var c = 0; c < headers.Length; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
            }

            for (var r = 0; r < rows.Length; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                {
                    sheet.Cell(r + 2, c + 1).Value = rows[r][c];
                }
            }

            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }

    private static FormFile MakeXlsx(byte[] bytes, string name = "questions.xlsx")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = _xlsxContentType,
        };
    }

    private static string[] SingleChoiceRow(string rowId, string stem, string correct = "A", string difficulty = "Easy", string tag = "")
    {
        return [rowId, "SingleChoice", stem, "Option A", "Option B", "Option C", "Option D", correct, "Explanation", difficulty, tag];
    }

    private static async Task<Quiz> SeedQuizAsync(ApplicationDbContext db, string instructorId, string title = "Quiz")
    {
        var course = new Course { Title = "Course", InstructorId = instructorId, Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        var quiz = new Quiz { CourseId = course.Id, Title = title, OrderIndex = 1 };
        db.Set<Quiz>().Add(quiz);
        await db.SaveChangesAsync();
        return quiz;
    }

    private static async Task<Question> SeedQuestionAsync(ApplicationDbContext db, int quizId, string stem, string? rowId = null)
    {
        var question = new Question
        {
            QuizId = quizId,
            Text = stem,
            QuestionType = QuestionType.SingleChoice,
            OrderIndex = 1,
            RowId = rowId,
        };
        question.AnswerOptions.Add(new AnswerOption { Text = "A", IsCorrect = true, OrderIndex = 1 });
        question.AnswerOptions.Add(new AnswerOption { Text = "B", IsCorrect = false, OrderIndex = 2 });
        db.Set<Question>().Add(question);
        await db.SaveChangesAsync();
        return question;
    }

    private static List<string[]> ReadRows(byte[] xlsx)
    {
        using var workbook = new XLWorkbook(new MemoryStream(xlsx));
        var sheet = workbook.Worksheets.First();
        var rows = new List<string[]>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var cells = new List<string>();
            for (var c = 1; c <= 12; c++)
            {
                cells.Add(sheet.Cell(r, c).GetString());
            }

            if (cells.Any(text => text.Length > 0))
            {
                rows.Add(cells.ToArray());
            }
        }

        return rows;
    }

    [Fact]
    public async Task Sync_import_creates_questions_and_reports_row_errors()
    {
        var (db, service, _, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");
            var valid = Enumerable.Range(0, 5).Select(i => SingleChoiceRow($"r{i}", $"Stem {i}")).ToArray();
            var invalid = new[]
            {
                new[] { "bad", "EssayType", "Bad type", "A", "B", "", "", "A", "", "Easy", "" },
                new[] { "", "SingleChoice", "", "A", "B", "", "", "A", "", "Easy", "" },
                new[] { "o1", "TrueFalse", "TF", "", "", "", "", "Maybe", "", "Easy", "" },
            };
            var outcome = await service.ImportAsync(MakeXlsx(BuildXlsx(false, valid.Concat(invalid).ToArray())), "instructor-1", quiz.Id, QuestionImportMode.Append, isBank: false, forceAsync: false);

            Assert.Equal(QuestionImportOutcomeKind.Completed, outcome.Kind);
            Assert.Equal(5, outcome.SuccessCount);
            Assert.Equal(3, outcome.Errors.Count);
            Assert.Contains(outcome.Errors, e => e.Field == "QuestionType");
            Assert.Contains(outcome.Errors, e => e.Field == "Stem");
            Assert.Contains(outcome.Errors, e => e.Field == "CorrectAnswer" && e.Message.Contains("True 或 False"));
            Assert.All(outcome.Errors, e => Assert.True(e.RowIndex >= 2));
            Assert.Equal(5, await db.Set<Question>().CountAsync(q => q.QuizId == quiz.Id));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Sync_import_rejects_wrong_extension_content_type_and_oversize()
    {
        var (db, service, _, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");

            var csv = new FormFile(new MemoryStream("a,b"u8.ToArray()), 0, 3, "file", "questions.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv",
            };
            var csvOutcome = await service.ImportAsync(csv, "instructor-1", quiz.Id, QuestionImportMode.Append, isBank: false, forceAsync: false);
            Assert.Equal(QuestionImportOutcomeKind.Error, csvOutcome.Kind);
            Assert.Contains(".xlsx", csvOutcome.Error);

            var big = new FormFile(new MemoryStream(new byte[12 * 1024 * 1024]), 0, 12 * 1024 * 1024, "file", "big.xlsx")
            {
                Headers = new HeaderDictionary(),
                ContentType = _xlsxContentType,
            };
            var bigOutcome = await service.ImportAsync(big, "instructor-1", quiz.Id, QuestionImportMode.Append, isBank: false, forceAsync: false);
            Assert.Equal(QuestionImportOutcomeKind.Error, bigOutcome.Kind);
            Assert.Contains("大小限制", bigOutcome.Error);
            Assert.Empty(new DirectoryInfo(tempDir).GetFiles());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Large_import_escalates_to_async_job()
    {
        var (db, service, _, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");
            var rows = Enumerable.Range(0, 250).Select(i => SingleChoiceRow($"r{i}", $"Stem {i}")).ToArray();
            var outcome = await service.ImportAsync(MakeXlsx(BuildXlsx(false, rows)), "instructor-1", quiz.Id, QuestionImportMode.Append, isBank: false, forceAsync: false);

            Assert.Equal(QuestionImportOutcomeKind.Submitted, outcome.Kind);
            Assert.NotNull(outcome.JobId);
            Assert.Empty(await db.Set<Question>().Where(q => q.QuizId == quiz.Id).ToListAsync());
            Assert.Single(await db.Set<QuestionImportJob>().ToListAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Async_processor_imports_rows_and_writes_error_file()
    {
        var (db, service, _, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");
            var valid = Enumerable.Range(0, 3).Select(i => SingleChoiceRow($"r{i}", $"Stem {i}")).ToArray();
            var invalid = new[] { new[] { "", "SingleChoice", "", "A", "B", "", "", "A", "", "Easy", "" } };
            var outcome = await service.ImportAsync(MakeXlsx(BuildXlsx(false, valid.Concat(invalid).ToArray())), "instructor-1", quiz.Id, QuestionImportMode.Append, isBank: false, forceAsync: true);

            Assert.Equal(QuestionImportOutcomeKind.Submitted, outcome.Kind);
            var job = await db.Set<AsyncIOJob>().FindAsync(outcome.JobId!.Value);
            Assert.NotNull(job);
            var meta = await db.Set<QuestionImportJob>().FirstOrDefaultAsync(j => j.AsyncIOJobId == job.Id);
            Assert.NotNull(meta);

            var stream = await new LocalStorageProvider(tempDir).OpenAsync(job.FileKey);
            Assert.NotNull(stream);
            using (stream)
            {
                var result = await service.ProcessAsync(job, stream, CancellationToken.None);
                Assert.True(result.Ok);
                Assert.Equal(4, result.TotalRows);
                Assert.Equal(3, result.SuccessRows);
            }

            Assert.Equal(3, await db.Set<Question>().CountAsync(q => q.QuizId == quiz.Id));
            var refreshedMeta = await db.Set<QuestionImportJob>().FindAsync(meta.Id);
            Assert.NotNull(refreshedMeta);
            Assert.Equal(QuestionImportJobStatus.Success, refreshedMeta.Status);
            Assert.Equal(1, refreshedMeta.ErrorRows);
            Assert.NotNull(refreshedMeta.ErrorFileKey);
            Assert.Single(await db.Set<QuestionRowError>().ToListAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Non_owner_cannot_import_into_someone_elses_quiz()
    {
        var (db, service, _, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");
            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(false, [SingleChoiceRow("r1", "Stem")])),
                "instructor-2",
                quiz.Id,
                QuestionImportMode.Append,
                isBank: false,
                forceAsync: false);

            Assert.Equal(QuestionImportOutcomeKind.Error, outcome.Kind);
            Assert.Empty(await db.Set<Question>().ToListAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Update_or_append_updates_owned_question_and_rejects_foreign()
    {
        var (db, service, _, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");
            await SeedQuestionAsync(db, quiz.Id, "Original", rowId: "q-1");

            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(false, [SingleChoiceRow("q-1", "Updated stem")])),
                "instructor-1",
                quiz.Id,
                QuestionImportMode.UpdateOrAppend,
                isBank: false,
                forceAsync: false);

            Assert.Equal(QuestionImportOutcomeKind.Completed, outcome.Kind);
            Assert.Equal(1, outcome.SuccessCount);
            Assert.Single(await db.Set<Question>().Where(q => q.QuizId == quiz.Id).ToListAsync());
            Assert.Equal("Updated stem", (await db.Set<Question>().FirstAsync(q => q.QuizId == quiz.Id)).Text);

            // A foreign owner importing the same RowId reports "not owner".
            var otherQuiz = await SeedQuizAsync(db, "instructor-2", "Other");
            var foreignOutcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(false, [SingleChoiceRow("q-1", "Hijack")])),
                "instructor-2",
                otherQuiz.Id,
                QuestionImportMode.UpdateOrAppend,
                isBank: false,
                forceAsync: false);

            Assert.Equal(QuestionImportOutcomeKind.Completed, foreignOutcome.Kind);
            Assert.Equal(0, foreignOutcome.SuccessCount);
            var error = Assert.Single(foreignOutcome.Errors);
            Assert.Equal("not owner", error.Message);
            Assert.Equal("RowId", error.Field);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Rate_limiter_blocks_the_sixth_attempt_within_an_hour()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var config = new SystemConfigService(db);
        var limiter = new QuestionImportRateLimiter(config);
        var userId = "rl-" + Guid.NewGuid().ToString("N");

        for (var i = 0; i < 5; i++)
        {
            var allowed = await limiter.CheckAsync(userId);
            Assert.True(allowed.Allowed);
        }

        var blocked = await limiter.CheckAsync(userId);
        Assert.False(blocked.Allowed);
        Assert.True(blocked.RetryAfterSeconds > 0);
    }

    [Fact]
    public async Task Export_filters_by_type_and_is_owner_scoped()
    {
        var (db, _, export, tempDir) = Create();
        try
        {
            var quiz = await SeedQuizAsync(db, "instructor-1");
            await SeedQuestionAsync(db, quiz.Id, "Q1", "r1");
            await SeedQuestionAsync(db, quiz.Id, "Q2", "r2");
            var tf = new Question
            {
                QuizId = quiz.Id,
                Text = "Q3",
                QuestionType = QuestionType.TrueFalse,
                OrderIndex = 3,
                RowId = "r3",
                Difficulty = QuestionDifficulty.Hard,
            };
            db.Set<Question>().Add(tf);
            await db.SaveChangesAsync();

            var filters = new QuestionExportFilters(quiz.Id, QuestionType.SingleChoice, null, null, false, null);
            var (bytes, error, rowCount) = await export.ExportSyncAsync(filters, "instructor-1", isAdmin: false);
            Assert.Null(error);
            Assert.Equal(2, rowCount);
            var rows = ReadRows(bytes!);
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.Equal("SingleChoice", row[1]));

            // Another instructor cannot export this quiz.
            var forbidden = await export.ExportSyncAsync(filters, "instructor-2", isAdmin: false);
            Assert.NotNull(forbidden.Error);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Bank_export_requires_admin_and_bank_import_creates_bank_rows()
    {
        var (db, import, export, tempDir) = Create();
        try
        {
            var bankRows = new[] { new[] { "b1", "SingleChoice", "Bank Q", "A", "B", "", "", "A", "", "Easy", "Tag", "Science" } };
            var outcome = await import.ImportAsync(
                MakeXlsx(BuildXlsx(true, bankRows)),
                "admin-1",
                quizId: null,
                QuestionImportMode.Append,
                isBank: true,
                forceAsync: false);

            Assert.Equal(QuestionImportOutcomeKind.Completed, outcome.Kind);
            Assert.Equal(1, outcome.SuccessCount);
            var created = await db.Set<Question>().FirstAsync(q => q.IsBank);
            Assert.True(created.IsBank);
            Assert.Equal("Science", created.BankTopic);
            Assert.Equal("Tag", created.KnowledgeTag);

            var filters = new QuestionExportFilters(null, null, null, null, true, null);
            var forbidden = await export.ExportSyncAsync(filters, "instructor-1", isAdmin: false);
            Assert.NotNull(forbidden.Error);
            var adminExport = await export.ExportSyncAsync(filters, "admin-1", isAdmin: true);
            Assert.Null(adminExport.Error);
            Assert.Equal(1, adminExport.RowCount);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Template_returns_valid_xlsx_bytes()
    {
        var bytes = QuestionTemplateService.GetTemplateBytes(includeBankTopic: false);
        Assert.NotEmpty(bytes);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);

        var bankBytes = QuestionTemplateService.GetTemplateBytes(includeBankTopic: true);
        using var workbook = new XLWorkbook(new MemoryStream(bankBytes));
        Assert.Equal("BankTopic", workbook.Worksheets.First().Cell(1, 12).GetString());
    }
}
