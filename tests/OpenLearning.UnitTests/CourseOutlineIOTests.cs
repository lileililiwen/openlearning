using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseOutlineIO.Models;
using OpenLearning.CourseOutlineIO.Services;
using OpenLearning.Data;
using OpenLearning.Logging.Services;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.CourseOutlineIO;

public sealed class CourseOutlineIOTests
{
    private const string _xlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static (ApplicationDbContext Db, OutlineImportService Import, OutlineExportService Export, StorageService Storage, string TempDir) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = Path.Combine(Path.GetTempPath(), "ol-outline-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        var asyncIO = new AsyncIOService(db, storage, TestNotificationService.Create(db));
        var config = new SystemConfigService(db);
        var import = new OutlineImportService(db, asyncIO, storage, config, new LogService(db));
        var export = new OutlineExportService(db);
        return (db, import, export, storage, tempDir);
    }

    private static async Task<Course> SeedCourseAsync(ApplicationDbContext db, string instructorId, string title = "Course")
    {
        var now = DateTime.UtcNow;
        var course = new Course
        {
            Title = title,
            InstructorId = instructorId,
            Status = CourseStatus.Published,
            Description = "d",
            Category = "c",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    private static byte[] BuildOutlineXlsx(params (string? ModuleTitle, int? ModuleOrder, string? LessonTitle, int? LessonOrder, string? ContentUrl)[] rows)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Outline");
            sheet.Cell(1, 1).Value = "ModuleTitle";
            sheet.Cell(1, 2).Value = "ModuleOrder";
            sheet.Cell(1, 3).Value = "LessonTitle";
            sheet.Cell(1, 4).Value = "LessonOrder";
            sheet.Cell(1, 5).Value = "LessonContentUrl";
            for (var r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                if (row.ModuleTitle is not null)
                {
                    sheet.Cell(r + 2, 1).Value = row.ModuleTitle;
                }

                if (row.ModuleOrder is int mo)
                {
                    sheet.Cell(r + 2, 2).Value = mo;
                }

                if (row.LessonTitle is not null)
                {
                    sheet.Cell(r + 2, 3).Value = row.LessonTitle;
                }

                if (row.LessonOrder is int lo)
                {
                    sheet.Cell(r + 2, 4).Value = lo;
                }

                if (row.ContentUrl is not null)
                {
                    sheet.Cell(r + 2, 5).Value = row.ContentUrl;
                }
            }

            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }

    private static FormFile MakeXlsx(byte[] bytes, string name = "outline.xlsx")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = _xlsxContentType,
        };
    }

    private static FormFile MakeCsv(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", "outline.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv",
        };
    }

    private static List<string[]> ReadRows(byte[] xlsx)
    {
        using var workbook = new XLWorkbook(new MemoryStream(xlsx));
        var sheet = workbook.Worksheets.First();
        var rows = new List<string[]>();
        for (var row = 1; row <= sheet.LastRowUsed()?.RowNumber(); row++)
        {
            var values = new List<string>();
            for (var col = 1; col <= sheet.LastColumnUsed()?.ColumnNumber(); col++)
            {
                values.Add(sheet.Cell(row, col).GetFormattedString());
            }

            rows.Add(values.ToArray());
        }

        return rows;
    }

    // ===== Template =====

    [Fact]
    public void Template_has_headers_and_sample_row()
    {
        var rows = ReadRows(OutlineTemplateService.GetTemplateBytes());
        Assert.Equal("ModuleTitle", rows[0][0]);
        Assert.Equal("ModuleOrder", rows[0][1]);
        Assert.Equal("LessonTitle", rows[0][2]);
        Assert.Equal("LessonOrder", rows[0][3]);
        Assert.Equal("LessonContentUrl", rows[0][4]);
        Assert.Equal("Module 1", rows[1][0]);
        Assert.Equal("1", rows[1][1]);
        Assert.Equal("Lesson 1", rows[1][2]);
    }

    // ===== Sync import =====

    [Fact]
    public async Task ImportSync_creates_modules_and_lessons()
    {
        var (db, import, _, _, _) = Create();
        var instructor = await SeedCourseAsync(db, "inst");
        var file = MakeXlsx(BuildOutlineXlsx(
            ("Module 1", 1, "Lesson 1", 1, "https://example.com/lecture.mp4"),
            ("Module 1", 1, "Lesson 2", 2, null),
            ("Module 2", 2, "Lesson 3", 1, null)));

        var outcome = await import.ImportAsync(file, "inst", instructor.Id, OutlineImportMode.Append, isAdmin: false, forceAsync: false);

        Assert.Equal(OutlineImportOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(3, outcome.SuccessRows);
        Assert.Empty(outcome.Errors);

        var modules = await db.Modules.OrderBy(m => m.OrderIndex).ToListAsync();
        Assert.Equal(2, modules.Count);
        Assert.Equal("Module 1", modules[0].Title);
        Assert.Equal(1, modules[0].OrderIndex);

        var lessons = await db.Lessons.OrderBy(l => l.OrderIndex).ToListAsync();
        Assert.Equal(3, lessons.Count);
        Assert.Equal("Lesson 1", lessons[0].Title);
        Assert.Equal("https://example.com/lecture.mp4", lessons[0].ContentUrlRef);
        Assert.Null(lessons[1].ContentUrlRef);
    }

    [Fact]
    public async Task ImportSync_partial_success_reports_errors()
    {
        var (db, import, _, _, _) = Create();
        var course = await SeedCourseAsync(db, "inst");
        var file = MakeXlsx(BuildOutlineXlsx(
            ("Module 1", 1, "Lesson 1", 1, null),
            (null, 1, "Lesson 2", 2, null)));

        var outcome = await import.ImportAsync(file, "inst", course.Id, OutlineImportMode.Append, isAdmin: false, forceAsync: false);

        Assert.Equal(OutlineImportOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(1, outcome.SuccessRows);
        Assert.Single(outcome.Errors);
        Assert.Equal("ModuleTitle", outcome.Errors[0].Field);
        Assert.Equal(3, outcome.Errors[0].RowIndex);
        Assert.Equal(1, await db.Lessons.CountAsync());
    }

    [Fact]
    public async Task ImportSync_duplicate_lesson_order_reported()
    {
        var (db, import, _, _, _) = Create();
        var course = await SeedCourseAsync(db, "inst");
        var file = MakeXlsx(BuildOutlineXlsx(
            ("Module 1", 1, "Lesson 1", 1, null),
            ("Module 1", 1, "Lesson 2", 1, null)));

        var outcome = await import.ImportAsync(file, "inst", course.Id, OutlineImportMode.Append, isAdmin: false, forceAsync: false);

        Assert.Equal(1, outcome.SuccessRows);
        Assert.Single(outcome.Errors);
        Assert.Contains("重复", outcome.Errors[0].Message);
        Assert.Equal(1, await db.Lessons.CountAsync());
    }

    [Fact]
    public async Task Import_non_owner_denied()
    {
        var (db, import, _, _, _) = Create();
        await SeedCourseAsync(db, "inst");
        var file = MakeXlsx(BuildOutlineXlsx(("M", 1, "L", 1, null)));

        var outcome = await import.ImportAsync(file, "other", 1, OutlineImportMode.Append, isAdmin: false, forceAsync: false);

        Assert.Equal(OutlineImportOutcomeKind.Error, outcome.Kind);
        Assert.NotNull(outcome.Message);
    }

    // ===== Replace mode =====

    [Fact]
    public async Task Replace_mode_wipes_and_repopulates_keeping_course()
    {
        var (db, import, _, _, _) = Create();
        var instructor = await SeedCourseAsync(db, "inst", "CourseX");
        var module = new Module { CourseId = instructor.Id, Title = "Old", OrderIndex = 1 };
        db.Modules.Add(module);
        await db.SaveChangesAsync();
        db.Lessons.Add(new Lesson { ModuleId = module.Id, Title = "Old Lesson", OrderIndex = 1 });
        db.Enrollments.Add(new EnrollmentEntity { StudentId = "s1", CourseId = instructor.Id, EnrolledAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var preview = await import.PreflightReplaceAsync(instructor.Id);
        Assert.Equal(1, preview.ModuleCount);
        Assert.Equal(1, preview.LessonCount);

        var file = MakeXlsx(BuildOutlineXlsx(("New", 1, "New Lesson", 1, null)));
        var outcome = await import.ImportAsync(file, "inst", instructor.Id, OutlineImportMode.Replace, isAdmin: false, forceAsync: false);

        Assert.Equal(OutlineImportOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(1, outcome.SuccessRows);
        Assert.Equal(1, await db.Modules.CountAsync());
        Assert.Equal("New", (await db.Modules.FirstAsync()).Title);
        Assert.Equal(1, await db.Lessons.CountAsync());
        Assert.Equal("New Lesson", (await db.Lessons.FirstAsync()).Title);
        Assert.Equal(1, await db.Enrollments.CountAsync());
        Assert.NotNull(await db.Courses.FirstOrDefaultAsync(c => c.Id == instructor.Id));
    }

    // ===== Export =====

    [Fact]
    public async Task Export_owner_returns_outline_rows()
    {
        var (db, _, export, _, _) = Create();
        var instructor = await SeedCourseAsync(db, "inst");
        var module = new Module { CourseId = instructor.Id, Title = "M1", OrderIndex = 1 };
        db.Modules.Add(module);
        await db.SaveChangesAsync();
        db.Lessons.Add(new Lesson { ModuleId = module.Id, Title = "L1", OrderIndex = 1, ContentUrlRef = "https://x/1.mp4" });
        await db.SaveChangesAsync();

        var (bytes, error) = await export.ExportAsync(instructor.Id, "inst", isAdmin: false);

        Assert.Null(error);
        var rows = ReadRows(bytes!);
        Assert.Equal("M1", rows[1][0]);
        Assert.Equal("1", rows[1][1]);
        Assert.Equal("L1", rows[1][2]);
        Assert.Equal("1", rows[1][3]);
        Assert.Equal("https://x/1.mp4", rows[1][4]);
    }

    [Fact]
    public async Task Export_non_owner_denied()
    {
        var (db, _, export, _, _) = Create();
        var instructor = await SeedCourseAsync(db, "inst");
        var (bytes, error) = await export.ExportAsync(instructor.Id, "other", isAdmin: false);
        Assert.Null(bytes);
        Assert.NotNull(error);
    }

    // ===== Async path =====

    [Fact]
    public async Task SubmitAndProcess_async_job_imports_and_mirrors_outcome()
    {
        var (db, import, _, _, _) = Create();
        var instructor = await SeedCourseAsync(db, "inst");
        var file = MakeXlsx(BuildOutlineXlsx(("M1", 1, "L1", 1, null)));

        var outcome = await import.ImportAsync(file, "inst", instructor.Id, OutlineImportMode.Append, isAdmin: false, forceAsync: true);

        Assert.Equal(OutlineImportOutcomeKind.Submitted, outcome.Kind);
        Assert.NotNull(outcome.JobId);

        var job = await db.Set<AsyncIOJob>().FirstAsync(j => j.Id == outcome.JobId);
        var (ok, error, total, success) = await import.ProcessAsync(job, new MemoryStream(BuildOutlineXlsx(("M1", 1, "L1", 1, null))), default);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(1, total);
        Assert.Equal(1, success);

        var meta = await db.Set<OutlineImportJob>().FirstAsync();
        Assert.Equal(OutlineImportJobStatus.Success, meta.Status);
        Assert.Equal(1, meta.SuccessRows);
        Assert.Equal(1, await db.Lessons.CountAsync());
        Assert.Contains(await db.OperationLogs.Select(l => l.Action).ToListAsync(), a => a == "OutlineImport");
    }

    // ===== File safety =====

    [Fact]
    public async Task Csv_file_rejected()
    {
        var (db, import, _, _, _) = Create();
        var course = await SeedCourseAsync(db, "inst");
        var outcome = await import.ImportAsync(MakeCsv("a,b,c"u8.ToArray()), "inst", course.Id, OutlineImportMode.Append, isAdmin: false, forceAsync: false);
        Assert.Equal(OutlineImportOutcomeKind.Error, outcome.Kind);
        Assert.Contains(".xlsx", outcome.Message);
    }

    [Fact]
    public async Task Oversized_file_rejected()
    {
        var (db, import, _, _, _) = Create();
        var course = await SeedCourseAsync(db, "inst");
        db.Settings.Add(new OpenLearning.SystemConfig.Models.Setting { Key = "courseOutline.import.maxBytes", Value = "1" });
        await db.SaveChangesAsync();

        var file = MakeXlsx(BuildOutlineXlsx(("M", 1, "L", 1, null)));
        var outcome = await import.ImportAsync(file, "inst", course.Id, OutlineImportMode.Append, isAdmin: false, forceAsync: false);

        Assert.Equal(OutlineImportOutcomeKind.Error, outcome.Kind);
        Assert.Contains("MB", outcome.Message);
    }
}
