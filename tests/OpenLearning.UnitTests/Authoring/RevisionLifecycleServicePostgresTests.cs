using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Authoring.Models;
using OpenLearning.Authoring.Services;
using Xunit;

namespace OpenLearning.UnitTests.Authoring;

/// <summary>
/// Maps only the authoring entities against PostgreSQL so we can exercise the
/// relational provider's optimistic-concurrency path (<see cref="DbUpdateConcurrencyException"/>),
/// which the InMemory provider silently ignores. Uses the dedicated local
/// <c>openlearning_integration</c> database.
/// </summary>
internal sealed class AuthoringPostgresContext : DbContext
{
    public AuthoringPostgresContext(DbContextOptions<AuthoringPostgresContext> options)
        : base(options)
    {
    }

    public DbSet<CourseRevision> CourseRevisions => Set<CourseRevision>();

    public DbSet<CourseRevisionPointer> CourseRevisionPointers => Set<CourseRevisionPointer>();

    public DbSet<RevisionValidationResult> RevisionValidationResults => Set<RevisionValidationResult>();

    public DbSet<RevisionAudit> RevisionAudits => Set<RevisionAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseRevision).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class RevisionLifecycleServicePostgresTests
{
    private const string _connectionString =
        "Host=localhost;Database=openlearning_integration;Username=openlearning;Password=openlearning_dev";

    private static int _courseCounter;

    private static async Task<AuthoringPostgresContext> NewDbAsync()
    {
        var options = new DbContextOptionsBuilder<AuthoringPostgresContext>()
            .UseNpgsql(_connectionString)
            .Options;
        var db = new AuthoringPostgresContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static int NextCourseId()
    {
        return System.Threading.Interlocked.Increment(ref _courseCounter);
    }

    private static string ValidSnapshot()
    {
        return "{\"Modules\":[{\"Title\":\"Module 1\",\"Lessons\":[{\"Title\":\"Lesson 1\",\"Type\":\"Video\"}]}]}";
    }

    private static async Task<RevisionLifecycleService> SeedPublishedAsync(
        AuthoringPostgresContext db, int courseId, string owner)
    {
        var service = new RevisionLifecycleService(db);
        await service.EnsureDraftAsync(courseId, owner);
        await service.EditDraftAsync(courseId, owner, new RevisionPatch
        {
            Title = "Published title",
            Summary = "A summary",
            Level = "Beginner",
            Language = "en",
            ContentSnapshotJson = ValidSnapshot(),
        });
        var result = await service.PublishAsync(courseId, owner);
        Assert.True(result.Succeeded);
        return service;
    }

    [Fact]
    public async Task PublishAsync_PromotesDraftAndRepointsPointer()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new RevisionLifecycleService(db);

        await service.EnsureDraftAsync(courseId, "owner-1");
        await service.EditDraftAsync(courseId, "owner-1", new RevisionPatch
        {
            Title = "T",
            Summary = "S",
            Level = "Beginner",
            Language = "en",
            ContentSnapshotJson = ValidSnapshot(),
        });

        var publish = await service.PublishAsync(courseId, "owner-1");

        Assert.True(publish.Succeeded);
        Assert.NotNull(publish.ActiveRevisionId);
        var active = await service.GetActiveRevisionAsync(courseId);
        Assert.NotNull(active);
        Assert.Equal(RevisionState.Published, active.State);
        Assert.Equal("T", active.Title);
    }

    [Fact]
    public async Task PublishAsync_RejectsInvalidDraft()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new RevisionLifecycleService(db);

        await service.EnsureDraftAsync(courseId, "owner-2");

        var publish = await service.PublishAsync(courseId, "owner-2");

        Assert.False(publish.Succeeded);
        Assert.NotEmpty(publish.Errors);
    }

    [Fact]
    public async Task ConcurrentPublish_OnlyOneSucceeds()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new RevisionLifecycleService(db);
        await service.EnsureDraftAsync(courseId, "owner-3");
        await service.EditDraftAsync(courseId, "owner-3", new RevisionPatch
        {
            Title = "T",
            Summary = "S",
            Level = "Beginner",
            Language = "en",
            ContentSnapshotJson = ValidSnapshot(),
        });

        await using var dbA = await NewDbAsync();
        await using var dbB = await NewDbAsync();
        var svcA = new RevisionLifecycleService(dbA);
        var svcB = new RevisionLifecycleService(dbB);

        // Load the pointer into both contexts so each holds the original RowVersion
        // tracked locally. This makes the second publish's repoint deterministically
        // conflict (WHERE RowVersion = original) regardless of thread scheduling,
        // exercising the relational optimistic-concurrency guard.
        await svcA.EnsureDraftAsync(courseId, "owner-3");
        await svcB.EnsureDraftAsync(courseId, "owner-3");

        Exception? exA = null;
        Exception? exB = null;
        PublishResult? resA = null;
        PublishResult? resB = null;

        var taskA = Task.Run(async () =>
        {
            try
            {
                resA = await svcA.PublishAsync(courseId, "owner-3");
            }
            catch (RevisionConcurrencyException e)
            {
                exA = e;
            }
        });
        var taskB = Task.Run(async () =>
        {
            try
            {
                resB = await svcB.PublishAsync(courseId, "owner-3");
            }
            catch (RevisionConcurrencyException e)
            {
                exB = e;
            }
        });

        await Task.WhenAll(taskA, taskB);

        var succeeded = (resA is { Succeeded: true } ? 1 : 0) + (resB is { Succeeded: true } ? 1 : 0);
        Assert.Equal(1, succeeded);
        Assert.True(exA is not null || exB is not null);
    }

    [Fact]
    public async Task RollbackAsync_CreatesNewDraftPreservingHistory()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = await SeedPublishedAsync(db, courseId, "owner-4");

        var activeBefore = await service.GetActiveRevisionAsync(courseId);
        Assert.NotNull(activeBefore);
        var targetId = activeBefore.Id;
        var targetTitle = activeBefore.Title;

        var draft = await service.RollbackAsync(courseId, "owner-4", targetId);

        Assert.Equal(RevisionState.Draft, draft.State);
        Assert.Equal(targetTitle, draft.Title);

        var activeAfter = await service.GetActiveRevisionAsync(courseId);
        Assert.NotNull(activeAfter);
        Assert.Equal(targetId, activeAfter.Id);

        var rolledBackAudit = await db.RevisionAudits.AsNoTracking()
            .FirstOrDefaultAsync(a => a.CourseId == courseId && a.Action == "RolledBack");
        Assert.NotNull(rolledBackAudit);
    }

    [Fact]
    public async Task GetActiveRevisionAsync_ReturnsOnlyPublishedRevision()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new RevisionLifecycleService(db);

        await service.EnsureDraftAsync(courseId, "owner-5");
        await service.EditDraftAsync(courseId, "owner-5", new RevisionPatch
        {
            Title = "T",
            Summary = "S",
            Level = "Beginner",
            Language = "en",
            ContentSnapshotJson = ValidSnapshot(),
        });
        await service.PublishAsync(courseId, "owner-5");

        var before = await service.GetActiveRevisionAsync(courseId);
        Assert.NotNull(before);
        var activeId = before.Id;

        await service.EditDraftAsync(courseId, "owner-5", new RevisionPatch { Title = "Mutated draft" });

        var after = await service.GetActiveRevisionAsync(courseId);
        Assert.NotNull(after);
        Assert.Equal(activeId, after.Id);
        Assert.Equal("T", after.Title);
    }

    [Fact]
    public async Task ValidateDraftAsync_RecordsValidationErrors()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new RevisionLifecycleService(db);

        await service.EnsureDraftAsync(courseId, "owner-6");

        var outcome = await service.ValidateDraftAsync(courseId, "owner-6");

        Assert.False(outcome.IsValid);
        Assert.NotEmpty(outcome.Errors);

        var recorded = await db.RevisionValidationResults.AsNoTracking()
            .FirstOrDefaultAsync(v => v.CourseId == courseId);
        Assert.NotNull(recorded);
        Assert.False(recorded.IsValid);
    }
}
