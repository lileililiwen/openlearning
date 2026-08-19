using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Storage.Services;
using OpenLearning.StudentIO.Models;
using OpenLearning.StudentIO.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.StudentIO;

public sealed class StudentIOTests
{
    private const string _xlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] _headers =
    [
        "Action", "Email", "Phone", "DisplayName", "Password", "CourseIds", "ClassGroupIds",
    ];

    private static (ApplicationDbContext Db, StudentImportService Service, Mock<UserManager<ApplicationUser>> Users, List<ApplicationUser> Created, string TempDir) Create(
        IClassAssignmentLookup? lookup = null)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ol-studio-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        var asyncIO = new AsyncIOService(db, storage, TestNotificationService.Create(db));
        var enrollments = new EnrollmentService(db);
        var notifications = TestNotificationService.Create(db);
        if (lookup is null)
        {
            var defaultLookup = new Mock<IClassAssignmentLookup>();
            defaultLookup.Setup(l => l.IsAssignedAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
            lookup = defaultLookup.Object;
        }

        var created = new List<ApplicationUser>();
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        manager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u =>
            {
                u.Id = "uid-" + Guid.NewGuid().ToString("N");
                created.Add(u);
            })
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((u, _) =>
            {
                u.Id = "uid-" + Guid.NewGuid().ToString("N");
                created.Add(u);
            })
            .ReturnsAsync(IdentityResult.Success);
        manager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        manager.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("reset-token-abc");

        var service = new StudentImportService(db, manager.Object, storage, asyncIO, enrollments, notifications, lookup);
        return (db, service, manager, created, tempDir);
    }

    private static byte[] BuildXlsx(params string[][] rows)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Students");
            for (var c = 0; c < _headers.Length; c++)
            {
                sheet.Cell(1, c + 1).Value = _headers[c];
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

    private static FormFile MakeXlsx(byte[] bytes, string name = "students.xlsx")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = _xlsxContentType,
        };
    }

    private static string[] CreateRow(int i)
    {
        return ["Create", $"student{i}@example.com", string.Empty, $"Student {i}", string.Empty, string.Empty, string.Empty];
    }

    private static async Task<Course> SeedCourseAsync(ApplicationDbContext db, decimal? price, int courseId, string title = "Course")
    {
        var course = new Course
        {
            Id = courseId,
            Title = title,
            Description = "desc",
            Category = "General",
            InstructorId = "instructor-1",
            Status = CourseStatus.Published,
            Price = price,
        };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    [Fact]
    public async Task Sync_create_creates_accounts_and_sends_welcome()
    {
        var (db, service, _, created, tempDir) = Create();
        try
        {
            var rows = Enumerable.Range(0, 5).Select(CreateRow).ToArray();
            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(rows)), "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.Create, forceAsync: false);

            Assert.Equal(StudentImportOutcomeKind.Completed, outcome.Kind);
            Assert.Equal(5, outcome.SuccessCount);
            Assert.Empty(outcome.Errors);
            Assert.Equal(5, created.Count);
            Assert.Equal(5, await db.Set<Notification>().CountAsync(n => n.Type == NotificationType.AccountWelcome));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Duplicate_emails_within_file_are_reported()
    {
        var (_, service, _, created, tempDir) = Create();
        try
        {
            var rows = new[]
            {
                new[] { "Create", "dup@example.com", "", "A", "", "", "" },
                new[] { "Create", "dup@example.com", "", "B", "", "", "" },
                CreateRow(1),
                CreateRow(2),
            };
            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(rows)), "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.Create, forceAsync: false);

            Assert.Equal(2, outcome.SuccessCount);
            Assert.Equal(2, outcome.Errors.Count);
            Assert.All(outcome.Errors, e => Assert.Equal("duplicate email", e.Message));
            Assert.Equal(2, created.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Create_with_existing_email_reports_already_in_use()
    {
        var (db, service, _, created, tempDir) = Create();
        try
        {
            db.Set<ApplicationUser>().Add(new ApplicationUser
            {
                Id = "existing-1",
                UserName = "existing@example.com",
                Email = "existing@example.com",
                NormalizedEmail = "EXISTING@EXAMPLE.COM",
                DisplayName = "Existing",
            });
            await db.SaveChangesAsync();

            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx([["Create", "existing@example.com", "", "New", "", "", ""]])),
                "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.Create, forceAsync: false);

            Assert.Equal(0, outcome.SuccessCount);
            var error = Assert.Single(outcome.Errors);
            Assert.Equal("email already in use", error.Message);
            Assert.Empty(created);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAndEnroll_into_paid_course_creates_account_but_reports_error()
    {
        var (db, service, _, created, tempDir) = Create();
        try
        {
            await SeedCourseAsync(db, price: 100m, courseId: 1);

            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx([["CreateAndEnroll", "paid@example.com", "", "Paid", "", "1", ""]])),
                "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.CreateAndEnroll, forceAsync: false);

            Assert.Equal(0, outcome.SuccessCount);
            var error = Assert.Single(outcome.Errors);
            Assert.Equal("course requires purchase", error.Message);
            Assert.Single(created);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAndEnroll_into_free_course_creates_account_and_enrollment()
    {
        var (db, service, _, created, tempDir) = Create();
        try
        {
            await SeedCourseAsync(db, price: null, courseId: 1);

            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx([["CreateAndEnroll", "free@example.com", "", "Free", "", "1", ""]])),
                "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.CreateAndEnroll, forceAsync: false);

            Assert.Equal(1, outcome.SuccessCount);
            Assert.Single(created);
            Assert.Single(await db.Set<EnrollmentEntity>().ToListAsync());
            Assert.Equal(1, await db.Set<Notification>().CountAsync(n => n.Type == NotificationType.AccountWelcome));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task EnrollExisting_missing_user_reports_not_found()
    {
        var (db, service, _, _, tempDir) = Create();
        try
        {
            await SeedCourseAsync(db, price: null, courseId: 1);

            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx([["EnrollExisting", "nobody@example.com", "", "", "", "1", ""]])),
                "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.EnrollExisting, forceAsync: false);

            var error = Assert.Single(outcome.Errors);
            Assert.Equal("user not found", error.Message);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task EnrollExisting_enrolls_existing_user_and_notifies()
    {
        var (db, service, _, _, tempDir) = Create();
        try
        {
            await SeedCourseAsync(db, price: null, courseId: 1);
            db.Set<ApplicationUser>().Add(new ApplicationUser
            {
                Id = "student-1",
                UserName = "existing@example.com",
                Email = "existing@example.com",
                NormalizedEmail = "EXISTING@EXAMPLE.COM",
                DisplayName = "Existing",
            });
            await db.SaveChangesAsync();

            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx([["EnrollExisting", "existing@example.com", "", "", "", "1", ""]])),
                "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.EnrollExisting, forceAsync: false);

            Assert.Equal(1, outcome.SuccessCount);
            Assert.Single(await db.Set<EnrollmentEntity>().ToListAsync());
            Assert.Equal(1, await db.Set<Notification>().CountAsync(n => n.Type == NotificationType.EnrollmentGrantedBulk));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Large_file_submits_async_job()
    {
        var (db, service, _, _, tempDir) = Create();
        try
        {
            var rows = Enumerable.Range(0, 250).Select(CreateRow).ToArray();
            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(rows)), "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.Create, forceAsync: false);

            Assert.Equal(StudentImportOutcomeKind.Submitted, outcome.Kind);
            Assert.NotNull(outcome.JobId);
            Assert.Single(await db.Set<StudentImportJob>().ToListAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Async_processor_imports_rows_and_writes_error_file()
    {
        var (db, service, _, created, tempDir) = Create();
        try
        {
            var rows = new[]
            {
                CreateRow(1),
                CreateRow(2),
                new[] { "Create", "dup@example.com", "", "Dup", "", "", "" },
                new[] { "Create", "dup@example.com", "", "Dup2", "", "", "" },
            };
            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(rows)), "admin-1", new StudentImportScope(IsTa: false), StudentRowAction.Create, forceAsync: true);

            var job = await db.Set<AsyncIOJob>().FindAsync(outcome.JobId!.Value);
            Assert.NotNull(job);
            var meta = await db.Set<StudentImportJob>().FirstOrDefaultAsync(j => j.AsyncIOJobId == job.Id);
            Assert.NotNull(meta);

            var stream = await new LocalStorageProvider(tempDir).OpenAsync(job.FileKey);
            Assert.NotNull(stream);
            using (stream)
            {
                var result = await service.ProcessAsync(job, stream, CancellationToken.None);
                Assert.True(result.Ok);
                Assert.Equal(4, result.TotalRows);
                Assert.Equal(2, result.SuccessRows);
            }

            var refreshedMeta = await db.Set<StudentImportJob>().FindAsync(meta.Id);
            Assert.NotNull(refreshedMeta);
            Assert.Equal(StudentImportJobStatus.Success, refreshedMeta.Status);
            Assert.Equal(2, refreshedMeta.ErrorRows);
            Assert.NotNull(refreshedMeta.ErrorFileKey);
            Assert.Equal(2, created.Count);
            Assert.Equal(2, await db.Set<StudentImportRowError>().CountAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Ta_scope_rejects_unassigned_class()
    {
        var lookup = new Mock<IClassAssignmentLookup>();
        lookup.Setup(l => l.IsAssignedAsync("ta-1", 10)).ReturnsAsync(true);
        lookup.Setup(l => l.IsAssignedAsync("ta-1", It.Is<int>(i => i != 10))).ReturnsAsync(false);

        var (db, service, _, _, tempDir) = Create(lookup.Object);
        try
        {
            await SeedCourseAsync(db, price: null, courseId: 1);
            var classGroup = new ClassGroup { Id = 10, CourseId = 1, Name = "Class A" };
            db.Set<ClassGroup>().Add(classGroup);
            await db.SaveChangesAsync();

            var rows = new[]
            {
                new[] { "CreateAndEnroll", "ta1@example.com", "", "TA One", "", "1", "10" },
                new[] { "CreateAndEnroll", "ta2@example.com", "", "TA Two", "", "1", "11" },
            };
            var outcome = await service.ImportAsync(
                MakeXlsx(BuildXlsx(rows)), "ta-1", new StudentImportScope(IsTa: true, RequiredClassGroupId: 10),
                StudentRowAction.CreateAndEnroll, forceAsync: false);

            Assert.Equal(1, outcome.SuccessCount);
            var error = Assert.Single(outcome.Errors);
            Assert.Equal("class not assigned", error.Message);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Template_returns_valid_xlsx_bytes()
    {
        var bytes = StudentImportTemplateService.GetTemplateBytes();
        Assert.NotEmpty(bytes);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal("Action", workbook.Worksheets.First().Cell(1, 1).GetString());
        Assert.Equal("ClassGroupIds", workbook.Worksheets.First().Cell(1, 7).GetString());
    }
}
