using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Data;
using OpenLearning.Storage.Services;
using Xunit;

namespace OpenLearning.UnitTests.AsyncIO;

public sealed class AsyncIOTests
{
    private sealed class CsvValidator : IIOFileValidator
    {
        public string[] AllowedExtensions { get; } = new[] { ".csv" };

        public long MaxBytes { get; } = 1_000_000;

        public string? Validate(IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return "Only .csv files are allowed.";
            }

            return file.Length > MaxBytes ? "File is too large." : null;
        }
    }

    private static FormFile MakeFile(string name, string content)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv",
        };
    }

    private static (ApplicationDbContext Db, AsyncIOService Service, string TempDir) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ol-asyncio-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        return (db, new AsyncIOService(db, storage, TestNotificationService.Create(db)), tempDir);
    }

    [Fact]
    public async Task Submit_rejects_bad_extension_and_oversize()
    {
        var (_, service, tempDir) = Create();
        try
        {
            var validator = new CsvValidator();
            var (rejected, error) = await service.SubmitAsync("u1", "test-import", validator, MakeFile("data.txt", "a,b"));
            Assert.Null(rejected);
            Assert.Contains(".csv", error, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(new DirectoryInfo(tempDir).GetFiles());

            var oversized = new FormFile(new MemoryStream(new byte[2_000_000]), 0, 2_000_000, "file", "big.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv",
            };
            var (oversizeRejected, oversizeError) = await service.SubmitAsync("u1", "test-import", validator, oversized);
            Assert.Null(oversizeRejected);
            Assert.Contains("large", oversizeError, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Submit_creates_pending_job_and_lifecycle_transitions()
    {
        var (db, service, tempDir) = Create();
        try
        {
            var (job, error) = await service.SubmitAsync("u1", "test-import", new CsvValidator(), MakeFile("data.csv", "a,b"));
            Assert.Null(error);
            Assert.NotNull(job);
            Assert.Equal(AsyncIOJobStatus.Pending, job.Status);

            await service.MarkRunningAsync(job.Id);
            var running = await db.Set<AsyncIOJob>().FindAsync(job.Id);
            Assert.NotNull(running);
            Assert.Equal(AsyncIOJobStatus.Running, running.Status);

            await service.CompleteAsync(job.Id, 10, 8, 2);
            var done = await db.Set<AsyncIOJob>().FindAsync(job.Id);
            Assert.NotNull(done);
            Assert.Equal(AsyncIOJobStatus.Success, done.Status);
            Assert.Equal(8, done.SuccessRows);
            Assert.Equal(2, done.ErrorRows);

            await service.FailAsync(job.Id, "boom");
            var failed = await db.Set<AsyncIOJob>().FindAsync(job.Id);
            Assert.NotNull(failed);
            Assert.Equal(AsyncIOJobStatus.Failed, failed.Status);
            Assert.Equal("boom", failed.ErrorMessage);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Owner_scoping_on_get_and_list()
    {
        var (_, service, tempDir) = Create();
        try
        {
            var (job, _) = await service.SubmitAsync("u1", "test-import", new CsvValidator(), MakeFile("data.csv", "a,b"));
            Assert.NotNull(await service.GetJobAsync(job!.Id, "u1", isAdmin: false));
            Assert.Null(await service.GetJobAsync(job.Id, "u2", isAdmin: false));
            Assert.NotNull(await service.GetJobAsync(job.Id, "u2", isAdmin: true));

            var own = await service.ListJobsAsync("u1", isAdmin: false);
            Assert.Single(own);
            var other = await service.ListJobsAsync("u2", isAdmin: false);
            Assert.Empty(other);
            var all = await service.ListJobsAsync(null, isAdmin: true);
            Assert.Single(all);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Cleanup_prunes_old_files_and_nulls_keys()
    {
        var (db, service, tempDir) = Create();
        try
        {
            var (job, _) = await service.SubmitAsync("u1", "test-import", new CsvValidator(), MakeFile("data.csv", "a,b"));
            Assert.NotNull(job);
            job.ResultFileKey = "result-key";
            job.ErrorFileKey = "error-key";
            job.CreatedAt = DateTime.UtcNow.AddDays(-10);
            await db.SaveChangesAsync();

            var deleted = new List<string>();
            var count = await service.CleanupExpiredAsync(retentionDays: 7, deleteFile: key => { deleted.Add(key); return Task.CompletedTask; });

            Assert.Equal(1, count);
            Assert.Equal(2, deleted.Count);
            Assert.Contains("result-key", deleted);
            Assert.Contains("error-key", deleted);
            var refreshed = await db.Set<AsyncIOJob>().FindAsync(job.Id);
            Assert.Null(refreshed!.ResultFileKey);
            Assert.Null(refreshed.ErrorFileKey);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
