using Microsoft.EntityFrameworkCore;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Gamification.Models;
using OpenLearning.Gamification.Services;
using Xunit;

namespace OpenLearning.UnitTests;

public sealed class GamificationTests
{
    [Fact]
    public async Task Duplicate_events_are_idempotent_and_daily_cap_is_auditable()
    {
        await using var db = CreateDb();
        var service = new GamificationService(db);
        await service.CreateRuleAsync("Completion", "lesson.completed", 7, 10);
        var now = DateTime.UtcNow;

        var first = await service.AwardTrustedEventAsync("student", "lesson.completed", "lesson:1", GamificationScopeKind.Platform, "", now);
        var duplicate = await service.AwardTrustedEventAsync("student", "lesson.completed", "lesson:1", GamificationScopeKind.Platform, "", now);
        var capped = await service.AwardTrustedEventAsync("student", "lesson.completed", "lesson:2", GamificationScopeKind.Platform, "", now);

        Assert.True(first.Created);
        Assert.Equal(7, first.Amount);
        Assert.False(duplicate.Created);
        Assert.Equal(3, capped.Amount);
        Assert.True(capped.WasCapped);
        Assert.Equal(10, await db.Set<GamificationPointEntry>().SumAsync(x => x.Amount));
    }

    [Fact]
    public async Task Retry_remains_idempotent_after_rule_version_changes()
    {
        await using var db = CreateDb();
        var service = new GamificationService(db);
        await service.CreateRuleAsync("Version one", "course.completed", 10, 100);
        Assert.True((await service.AwardTrustedEventAsync("student", "course.completed", "course:1", GamificationScopeKind.Course, "1")).Created);
        await service.CreateRuleAsync("Version two", "course.completed", 20, 100);

        var retry = await service.AwardTrustedEventAsync("student", "course.completed", "course:1", GamificationScopeKind.Course, "1");

        Assert.False(retry.Created);
        Assert.Single(await db.Set<GamificationPointEntry>().ToListAsync());
    }

    [Fact]
    public async Task Badge_award_preserves_original_criteria_and_evidence()
    {
        await using var db = CreateDb();
        var service = new GamificationService(db);
        await service.CreateRuleAsync("Completion", "course.completed", 20, 100);
        var firstBadge = await service.CreateBadgeVersionAsync("finisher", "Finisher", 10, true);
        await service.AwardTrustedEventAsync("student", "course.completed", "course:1", GamificationScopeKind.Platform, "");
        var award = await db.Set<BadgeAward>().SingleAsync();

        await service.CreateBadgeVersionAsync("finisher", "Advanced finisher", 50, true);

        Assert.Equal(firstBadge.Id, award.BadgeDefinitionId);
        Assert.Equal(1, award.CriteriaVersion);
        Assert.Contains("criteria:10", award.Evidence, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Correction_is_compensating_and_does_not_touch_commerce_points()
    {
        await using var db = CreateDb();
        var service = new GamificationService(db);
        await service.CreateRuleAsync("Completion", "course.completed", 20, 100);
        await service.AwardTrustedEventAsync("student", "course.completed", "course:1", GamificationScopeKind.Platform, "");
        var original = await db.Set<GamificationPointEntry>().SingleAsync();
        db.Add(new PointsLedger { UserId = "student", Amount = 99, Reason = "commerce loyalty" });
        await db.SaveChangesAsync();

        var result = await service.CorrectAsync(original.Id, 5, "invalid source");

        Assert.True(result.Ok);
        Assert.Equal(-15, result.Entry!.Amount);
        Assert.Equal(5, await db.Set<GamificationPointEntry>().SumAsync(x => x.Amount));
        Assert.Equal(99, await db.Set<PointsLedger>().SumAsync(x => x.Amount));
    }

    [Fact]
    public async Task Leaderboard_excludes_opt_out_supports_ties_and_denies_cross_scope()
    {
        await using var db = CreateDb();
        var service = new GamificationService(db);
        await service.CreateRuleAsync("Completion", "course.completed", 10, 100);
        await service.SetPreferenceAsync("one", true, "Alpha");
        await service.SetPreferenceAsync("two", true, "Beta");
        await service.SetPreferenceAsync("hidden", false, string.Empty);
        await service.AwardTrustedEventAsync("one", "course.completed", "one:1", GamificationScopeKind.Course, "42");
        await service.AwardTrustedEventAsync("two", "course.completed", "two:1", GamificationScopeKind.Course, "42");
        await service.AwardTrustedEventAsync("hidden", "course.completed", "hidden:1", GamificationScopeKind.Course, "42");

        var board = await service.GetLeaderboardAsync(GamificationScopeKind.Course, "42", true);
        var denied = await service.GetLeaderboardAsync(GamificationScopeKind.Course, "99", false);
        await service.SetPreferenceAsync("two", false, string.Empty);
        var afterOptOut = await service.GetLeaderboardAsync(GamificationScopeKind.Course, "42", true);

        Assert.True(board.Ok);
        Assert.Equal(2, board.Rows.Count);
        Assert.All(board.Rows, row => Assert.Equal(1, row.Rank));
        Assert.False(denied.Ok);
        Assert.Empty(denied.Rows);
        Assert.Single(afterOptOut.Rows);
        Assert.Equal("Alpha", afterOptOut.Rows[0].Alias);
    }

    private static TestDb CreateDb()
    {
        return new TestDb(new DbContextOptionsBuilder<TestDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PointRule>();
            modelBuilder.Entity<GamificationPointEntry>();
            modelBuilder.Entity<BadgeDefinition>();
            modelBuilder.Entity<BadgeAward>();
            modelBuilder.Entity<GamificationChallenge>();
            modelBuilder.Entity<LeaderboardPreference>();
            modelBuilder.Entity<LeaderboardModeration>();
            modelBuilder.Entity<PointsLedger>();
        }
    }
}
