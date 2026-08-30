using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Outcomes.Models;
using OpenLearning.Outcomes.Services;
using Xunit;

namespace OpenLearning.UnitTests.Outcomes;

/// <summary>
/// Maps only the Outcomes entities against PostgreSQL to exercise the relational
/// provider (unique indexes, upsert semantics) that the InMemory provider ignores.
/// Uses the dedicated local <c>openlearning_integration</c> database.
/// </summary>
internal sealed class OutcomesPostgresContext : DbContext
{
    public OutcomesPostgresContext(DbContextOptions<OutcomesPostgresContext> options)
        : base(options)
    {
    }

    public DbSet<CourseOutcome> CourseOutcome => Set<CourseOutcome>();

    public DbSet<OutcomeActivityMapping> OutcomeActivityMapping => Set<OutcomeActivityMapping>();

    public DbSet<MasteryResult> MasteryResult => Set<MasteryResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseOutcome).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class OutcomeOperationsServicePostgresTests
{
    private const string _connectionString =
        "Host=localhost;Database=openlearning_integration_outcomes;Username=openlearning;Password=openlearning_dev";

    private static int _courseCounter;

    private static async Task<OutcomesPostgresContext> NewDbAsync()
    {
        var options = new DbContextOptionsBuilder<OutcomesPostgresContext>()
            .UseNpgsql(_connectionString)
            .Options;
        var db = new OutcomesPostgresContext(options);
        // Start from a clean database so unique indexes never collide with leftovers
        // from a previous test run against the shared local integration database.
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static int NextCourseId()
    {
        return System.Threading.Interlocked.Increment(ref _courseCounter);
    }

    [Fact]
    public async Task CreateOutcomeAndMapping_PersistRelationally()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new OutcomeOperationsService(db);

        var outcome = (await service.CreateOutcomeAsync(courseId, "owner-1", new CreateOutcomeRequest
        {
            Title = "Relational outcome",
            MasteryThreshold = 0.6,
        })).Outcome!;

        var mapping = await service.AddOutcomeActivityMappingAsync(courseId, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 2,
        });

        Assert.True(mapping.Succeeded);
        var storedOutcome = await db.CourseOutcome.AsNoTracking().SingleAsync(o => o.CourseId == courseId);
        var storedMapping = await db.OutcomeActivityMapping.AsNoTracking().SingleAsync(m => m.OutcomeId == outcome.Id);
        Assert.Equal(2, storedMapping.Weight);
        Assert.Equal("owner-1", storedOutcome.OwnerId);
    }

    [Fact]
    public async Task CalculateWithAsync_PersistsSingleCurrentResult_AndIsIdempotent()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(courseId, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(courseId, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });

        var results = new List<ActivityResult>
        {
            new() { ActivityId = 10, ActivityType = "Quiz", ScoreFraction = 0.8, Attempts = 1 },
        };

        await service.CalculateWithAsync(courseId, outcome.Id, "learner-1", results, "owner-1");
        await service.CalculateWithAsync(courseId, outcome.Id, "learner-1", results, "owner-1");

        var rows = await db.MasteryResult.AsNoTracking().Where(r => r.CourseId == courseId && r.LearnerId == "learner-1").ToListAsync();
        Assert.Single(rows);
        Assert.Equal(MasteryState.Mastered, rows[0].State);
    }

    [Fact]
    public async Task CalculateWithAsync_UnauthorizedActor_Throws()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(courseId, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;

        await Assert.ThrowsAsync<UnauthorizedOutcomeOperationException>(async () =>
            await service.CalculateWithAsync(courseId, outcome.Id, "learner-1", Array.Empty<ActivityResult>(), "intruder"));
    }

    [Fact]
    public async Task AddOutcomeActivityMappingAsync_RejectsNonPositiveWeightRelationally()
    {
        var courseId = NextCourseId();
        await using var db = await NewDbAsync();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(courseId, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;

        var result = await service.AddOutcomeActivityMappingAsync(courseId, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = -1,
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "Positive");
    }
}
