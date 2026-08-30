using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Outcomes.Models;
using OpenLearning.Outcomes.Services;
using Xunit;

namespace OpenLearning.UnitTests.Outcomes;

/// <summary>Maps only the Outcomes entities against an in-memory store for fast unit tests.</summary>
internal sealed class OutcomesTestContext : DbContext
{
    public OutcomesTestContext(DbContextOptions<OutcomesTestContext> options)
        : base(options)
    {
    }

    public DbSet<CourseOutcome> CourseOutcomes => Set<CourseOutcome>();

    public DbSet<OutcomeActivityMapping> OutcomeActivityMappings => Set<OutcomeActivityMapping>();

    public DbSet<MasteryResult> MasteryResults => Set<MasteryResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseOutcome).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class OutcomeOperationsServiceTests
{
    private static OutcomesTestContext NewDb()
    {
        return new OutcomesTestContext(
            new DbContextOptionsBuilder<OutcomesTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    [Fact]
    public async Task CreateOutcomeAsync_StoresOutcomeWithOwnerAndThreshold()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);

        var result = await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "Can explain ACID",
            MasteryThreshold = 0.8,
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Outcome);
        var stored = await db.Set<CourseOutcome>().AsNoTracking().SingleAsync();
        Assert.Equal("owner-1", stored.OwnerId);
        Assert.Equal(0.8, stored.MasteryThreshold);
    }

    [Fact]
    public async Task CreateOutcomeAsync_RejectsInvalidThreshold()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);

        var tooHigh = await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 1.5,
        });
        var noTitle = await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = string.Empty,
            MasteryThreshold = 0.7,
        });

        Assert.False(tooHigh.Succeeded);
        Assert.False(noTitle.Succeeded);
        Assert.Contains(tooHigh.Errors, e => e.Field == nameof(CreateOutcomeRequest.MasteryThreshold));
        Assert.Contains(noTitle.Errors, e => e.Field == nameof(CreateOutcomeRequest.Title));
    }

    [Fact]
    public async Task AddOutcomeActivityMappingAsync_RejectsNonPositiveWeightAndDuplicate()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;

        var zeroWeight = await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 0,
        });
        Assert.False(zeroWeight.Succeeded);
        Assert.Contains(zeroWeight.Errors, e => e.Code == "Positive");

        var badType = await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Essay",
            ActivityId = 10,
            Weight = 1,
        });
        Assert.False(badType.Succeeded);
        Assert.Contains(badType.Errors, e => e.Code == "InvalidActivityType");

        var first = await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });
        Assert.True(first.Succeeded);

        var duplicate = await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });
        Assert.False(duplicate.Succeeded);
        Assert.Contains(duplicate.Errors, e => e.Code == "Duplicate");
    }

    [Fact]
    public async Task CalculateWithAsync_LearnerAtThreshold_IsMastered()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });

        var results = new List<ActivityResult>
        {
            new() { ActivityId = 10, ActivityType = "Quiz", ScoreFraction = 0.9, Attempts = 1 },
        };
        var evaluation = await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "owner-1");

        Assert.Equal(MasteryState.Mastered, evaluation.Mastery.State);
        Assert.Equal(0.9, evaluation.Mastery.Score, 3);
        var stored = await db.Set<MasteryResult>().AsNoTracking().SingleAsync();
        Assert.Equal(MasteryState.Mastered, stored.State);
    }

    [Fact]
    public async Task CalculateWithAsync_BoundaryThreshold_IsMastered()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });

        var results = new List<ActivityResult>
        {
            new() { ActivityId = 10, ActivityType = "Quiz", ScoreFraction = 0.7, Attempts = 1 },
        };
        var evaluation = await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "owner-1");

        Assert.Equal(MasteryState.Mastered, evaluation.Mastery.State);
    }

    [Fact]
    public async Task CalculateWithAsync_RecalculationIsIdempotent()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });

        var results = new List<ActivityResult>
        {
            new() { ActivityId = 10, ActivityType = "Quiz", ScoreFraction = 0.5, Attempts = 1 },
        };
        await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "owner-1");
        await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "owner-1");

        var count = await db.Set<MasteryResult>().AsNoTracking().CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CalculateWithAsync_UnauthorizedActor_Throws()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;

        var results = new List<ActivityResult>();
        await Assert.ThrowsAsync<UnauthorizedOutcomeOperationException>(async () =>
            await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "intruder"));
    }

    [Fact]
    public async Task CalculateWithAsync_ProducesExplainableInterventionSignals()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });

        var results = new List<ActivityResult>
        {
            new() { ActivityId = 10, ActivityType = "Quiz", ScoreFraction = 0.3, Attempts = 4, LastAttemptAt = DateTime.UtcNow.AddDays(-40) },
        };
        var evaluation = await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "owner-1");

        // Below threshold + repeated low attempts + stale => NeedsReview (not just Developing).
        Assert.Equal(MasteryState.NeedsReview, evaluation.Mastery.State);
        Assert.Contains(evaluation.Signals, s => s.Rule == "RepeatedLowAttempts");
        Assert.Contains(evaluation.Signals, s => s.Rule == "StaleProgress");
        Assert.Contains(evaluation.Signals, s => s.Rule == "UnmetOutcome");
    }

    [Fact]
    public async Task CalculateWithAsync_MissingWorkSignalWhenMappingHasNoResult()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Assignment",
            ActivityId = 20,
            Weight = 1,
        });

        var results = new List<ActivityResult>
        {
            new() { ActivityId = 10, ActivityType = "Quiz", ScoreFraction = 1.0, Attempts = 1 },
        };
        var evaluation = await service.CalculateWithAsync(1, outcome.Id, "learner-1", results, "owner-1");

        Assert.Equal(MasteryState.Mastered, evaluation.Mastery.State);
        Assert.Contains(evaluation.Signals, s => s.Rule == "MissingWork" && s.Evidence.Contains("20"));
    }

    [Fact]
    public async Task EvaluateAsync_AllowsLearnerSelfViewWithoutOwnerCheck()
    {
        await using var db = NewDb();
        var service = new OutcomeOperationsService(db);
        var outcome = (await service.CreateOutcomeAsync(1, "owner-1", new CreateOutcomeRequest
        {
            Title = "T",
            MasteryThreshold = 0.7,
        })).Outcome!;
        await service.AddOutcomeActivityMappingAsync(1, "owner-1", new AddMappingRequest
        {
            OutcomeId = outcome.Id,
            ActivityType = "Quiz",
            ActivityId = 10,
            Weight = 1,
        });

        // No results and no activity source registered => NotStarted, no exception, no write.
        var evaluation = await service.EvaluateAsync(1, outcome.Id, "learner-1");

        Assert.Equal(MasteryState.NotStarted, evaluation.Mastery.State);
        var stored = await db.Set<MasteryResult>().AsNoTracking().CountAsync();
        Assert.Equal(0, stored);
    }
}
