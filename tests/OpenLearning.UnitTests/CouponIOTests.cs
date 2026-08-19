using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.CouponIO.Models;
using OpenLearning.CouponIO.Services;
using OpenLearning.Data;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Logging.Services;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;
using Xunit;

namespace OpenLearning.UnitTests.CouponIO;

public sealed class CouponIOTests
{
    private const string _xlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static (ApplicationDbContext Db, CouponImportService Service, StorageService Storage, string TempDir) Create()
    {
        CouponImportRateLimiter.Reset();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = Path.Combine(Path.GetTempPath(), "ol-coupon-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        var asyncIO = new AsyncIOService(db, storage, TestNotificationService.Create(db));
        var config = new SystemConfigService(db);
        var rateLimiter = new CouponImportRateLimiter(config);
        var service = new CouponImportService(db, asyncIO, storage, config, new LogService(db), rateLimiter);
        return (db, service, storage, tempDir);
    }

    private static byte[] BuildCouponXlsx(params (string? Code, string? Type, string? Value, string? From, string? To, string? Max)[] rows)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Coupons");
            sheet.Cell(1, 1).Value = "Code";
            sheet.Cell(1, 2).Value = "DiscountType";
            sheet.Cell(1, 3).Value = "DiscountValue";
            sheet.Cell(1, 4).Value = "ValidFrom";
            sheet.Cell(1, 5).Value = "ValidTo";
            sheet.Cell(1, 6).Value = "MaxRedemptions";
            for (var r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                sheet.Cell(r + 2, 1).Value = row.Code;
                sheet.Cell(r + 2, 2).Value = row.Type;
                sheet.Cell(r + 2, 3).Value = row.Value;
                if (row.From is not null)
                {
                    sheet.Cell(r + 2, 4).Value = DateTime.SpecifyKind(DateTime.Parse(row.From, CultureInfo.InvariantCulture), DateTimeKind.Utc);
                }

                if (row.To is not null)
                {
                    sheet.Cell(r + 2, 5).Value = DateTime.SpecifyKind(DateTime.Parse(row.To, CultureInfo.InvariantCulture), DateTimeKind.Utc);
                }

                if (row.Max is not null)
                {
                    sheet.Cell(r + 2, 6).Value = row.Max;
                }
            }

            workbook.SaveAs(stream);
        }

        return stream.ToArray();
    }

    private static FormFile MakeXlsx(byte[] bytes, string name = "coupons.xlsx")
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
        return new FormFile(stream, 0, stream.Length, "file", "coupons.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv",
        };
    }

    // ===== Template =====

    [Fact]
    public void Template_has_headers_and_sample_row()
    {
        using var workbook = new XLWorkbook(new MemoryStream(CouponImportTemplateService.GetTemplateBytes()));
        var sheet = workbook.Worksheets.First();
        Assert.Equal("Code", sheet.Cell(1, 1).GetFormattedString());
        Assert.Equal("DiscountType", sheet.Cell(1, 2).GetFormattedString());
        Assert.Equal("DiscountValue", sheet.Cell(1, 3).GetFormattedString());
        Assert.Equal("ValidFrom", sheet.Cell(1, 4).GetFormattedString());
        Assert.Equal("ValidTo", sheet.Cell(1, 5).GetFormattedString());
        Assert.Equal("MaxRedemptions", sheet.Cell(1, 6).GetFormattedString());
        Assert.Equal("SUMMER10", sheet.Cell(2, 1).GetFormattedString());
    }

    // ===== Sync import =====

    [Fact]
    public async Task ImportSync_creates_coupons()
    {
        var (db, service, _, _) = Create();
        var rows = Enumerable.Range(1, 100)
            .Select(i => (Code: (string?)($"CODE{i:D4}"), Type: (string?)"Percent", Value: (string?)"10", From: (string?)null, To: (string?)null, Max: (string?)"50"))
            .ToArray();
        var file = MakeXlsx(BuildCouponXlsx(rows));

        var outcome = await service.ImportAsync(file, "admin", forceAsync: false);

        Assert.Equal(CouponImportOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(100, outcome.SuccessRows);
        Assert.Empty(outcome.Errors);
        Assert.Equal(100, await db.Coupons.CountAsync());
        Assert.Equal("CODE0001", (await db.Coupons.FirstAsync()).Code);
    }

    [Fact]
    public async Task ImportSync_existing_codes_reported_not_overwritten()
    {
        var (db, service, _, _) = Create();
        db.Coupons.Add(new Coupon { Code = "EXIST001", DiscountPercent = 10, IsActive = true });
        await db.SaveChangesAsync();
        var file = MakeXlsx(BuildCouponXlsx(
            ("EXIST001", "Percent", "20", null, null, null),
            ("NEW0001", "Percent", "20", null, null, null)));

        var outcome = await service.ImportAsync(file, "admin", forceAsync: false);

        Assert.Equal(1, outcome.SuccessRows);
        Assert.Single(outcome.Errors);
        Assert.Contains("已存在", outcome.Errors[0].Message);
        var existing = await db.Coupons.FirstAsync(c => c.Code == "EXIST001");
        Assert.Equal(10, existing.DiscountPercent); // not overwritten
        Assert.Equal(2, await db.Coupons.CountAsync());
    }

    [Fact]
    public async Task ImportSync_duplicate_within_file_flags_both_rows()
    {
        var (db, service, _, _) = Create();
        var file = MakeXlsx(BuildCouponXlsx(
            ("DUP0001", "Percent", "10", null, null, null),
            ("DUP0001", "Amount", "5", null, null, null)));

        var outcome = await service.ImportAsync(file, "admin", forceAsync: false);

        Assert.Equal(0, outcome.SuccessRows);
        Assert.Equal(2, outcome.Errors.Count);
        Assert.All(outcome.Errors, e => Assert.Contains("重复", e.Message));
        Assert.Equal(0, await db.Coupons.CountAsync());
    }

    [Fact]
    public async Task ImportSync_invalid_code_format_and_date_range_reported()
    {
        var (db, service, _, _) = Create();
        var file = MakeXlsx(BuildCouponXlsx(
            ("BAD CODE!", "Percent", "10", null, null, null),
            ("RANGE001", "Percent", "10", "2026-06-01", "2026-01-01", null)));

        var outcome = await service.ImportAsync(file, "admin", forceAsync: false);

        Assert.Equal(0, outcome.SuccessRows);
        Assert.Equal(2, outcome.Errors.Count);
        Assert.Contains(outcome.Errors, e => e.Field == "Code");
        Assert.Contains(outcome.Errors, e => e.Field == "ValidFrom");
        Assert.Equal(0, await db.Coupons.CountAsync());
    }

    [Fact]
    public async Task Import_non_xlsx_and_empty_rejected()
    {
        var (_, service, _, _) = Create();
        var csv = await service.ImportAsync(MakeCsv("a,b"u8.ToArray()), "admin", forceAsync: false);
        Assert.Equal(CouponImportOutcomeKind.Error, csv.Kind);
        Assert.Contains(".xlsx", csv.Message);

        var empty = await service.ImportAsync(MakeXlsx(BuildCouponXlsx()), "admin", forceAsync: false);
        Assert.Equal(CouponImportOutcomeKind.Error, empty.Kind);
    }

    // ===== Rate limit =====

    [Fact]
    public async Task Rate_limit_blocks_sixth_import()
    {
        var (_, service, _, _) = Create();
        var file = MakeXlsx(BuildCouponXlsx(("CODE0001", "Percent", "10", null, null, null)));

        for (var i = 0; i < 5; i++)
        {
            var allowed = await service.ImportAsync(file, "admin", forceAsync: false);
            Assert.Equal(CouponImportOutcomeKind.Completed, allowed.Kind);
        }

        var blocked = await service.ImportAsync(file, "admin", forceAsync: false);
        Assert.Equal(CouponImportOutcomeKind.RateLimited, blocked.Kind);
        Assert.NotNull(blocked.RetryAfterSeconds);
    }

    // ===== Async path =====

    [Fact]
    public async Task SubmitAndProcess_async_job_imports_and_mirrors_outcome()
    {
        var (db, service, _, _) = Create();
        var file = MakeXlsx(BuildCouponXlsx(("CODE0001", "Percent", "10", null, null, null)));

        var outcome = await service.ImportAsync(file, "admin", forceAsync: true);
        Assert.Equal(CouponImportOutcomeKind.Submitted, outcome.Kind);
        Assert.NotNull(outcome.JobId);

        var job = await db.Set<AsyncIOJob>().FirstAsync(j => j.Id == outcome.JobId);
        var (ok, error, total, success) = await service.ProcessAsync(job, new MemoryStream(BuildCouponXlsx(("CODE0001", "Percent", "10", null, null, null))), default);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(1, total);
        Assert.Equal(1, success);

        var meta = await db.Set<CouponImportJob>().FirstAsync();
        Assert.Equal(CouponImportJobStatus.Success, meta.Status);
        Assert.Equal(1, meta.SuccessRows);
        Assert.Equal(1, await db.Coupons.CountAsync());
        Assert.Contains(await db.OperationLogs.Select(l => l.Action).ToListAsync(), a => a == "CouponImport");
    }

    [Fact]
    public async Task Async_process_writes_error_file_on_collision()
    {
        var (db, service, _, _) = Create();
        db.Coupons.Add(new Coupon { Code = "COLLID1", DiscountPercent = 10, IsActive = true });
        await db.SaveChangesAsync();
        var file = MakeXlsx(BuildCouponXlsx(("COLLID1", "Percent", "20", null, null, null)));

        var outcome = await service.ImportAsync(file, "admin", forceAsync: true);
        var job = await db.Set<AsyncIOJob>().FirstAsync(j => j.Id == outcome.JobId);
        var (ok, _, _, _) = await service.ProcessAsync(job, new MemoryStream(BuildCouponXlsx(("COLLID1", "Percent", "20", null, null, null))), default);

        Assert.True(ok);
        var meta = await db.Set<CouponImportJob>().FirstAsync();
        Assert.Equal(CouponImportJobStatus.Success, meta.Status);
        Assert.Equal(1, meta.ErrorRows);
        Assert.NotNull(meta.ErrorFileKey);
    }
}
