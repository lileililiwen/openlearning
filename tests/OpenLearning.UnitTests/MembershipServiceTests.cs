using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Memberships.Models;
using OpenLearning.Memberships.Services;
using Xunit;

namespace OpenLearning.UnitTests.Memberships;

public sealed class MembershipServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<int> CreatePlanAsync(ApplicationDbContext db, int durationDays = 30)
    {
        var service = new MembershipService(db);
        var (ok, error) = await service.CreatePlanAsync("Monthly", "All access", 9.99m, durationDays);
        Assert.True(ok);
        Assert.Null(error);
        return (await service.GetAllPlansAsync())[0].Id;
    }

    [Fact]
    public async Task CreatePlanAsync_lists_plan_and_rejects_short_duration()
    {
        var db = CreateDb();
        var service = new MembershipService(db);

        var (bad, badError) = await service.CreatePlanAsync("Bad", "x", 5m, 0);
        Assert.False(bad);
        Assert.NotNull(badError);

        var planId = await CreatePlanAsync(db);
        Assert.Single(await service.GetPlansAsync());
        Assert.Equal("Monthly", (await service.GetPlanByIdAsync(planId))!.Name);
    }

    [Fact]
    public async Task PurchaseAsync_creates_active_membership()
    {
        var db = CreateDb();
        var planId = await CreatePlanAsync(db, 30);
        var service = new MembershipService(db);

        var (ok, error) = await service.PurchaseAsync("u1", planId);

        Assert.True(ok);
        Assert.Null(error);
        Assert.True(await service.IsActiveAsync("u1"));
        var active = await service.GetActiveAsync("u1");
        Assert.NotNull(active);
        Assert.True(active.ExpiresAt > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task RenewAsync_extends_expiry()
    {
        var db = CreateDb();
        var planId = await CreatePlanAsync(db, 30);
        var service = new MembershipService(db);
        await service.PurchaseAsync("u1", planId);
        var before = (await service.GetActiveAsync("u1"))!.ExpiresAt;

        await service.PurchaseAsync("u1", planId);

        var after = (await service.GetActiveAsync("u1"))!.ExpiresAt;
        Assert.True(after > before.AddDays(29));
        Assert.Single(db.Set<Membership>()); // renewed, not a second row
    }

    [Fact]
    public async Task PurchaseAsync_inactive_plan_fails()
    {
        var db = CreateDb();
        var planId = await CreatePlanAsync(db);
        var service = new MembershipService(db);
        await service.SetPlanActiveAsync(planId, false);

        var (ok, error) = await service.PurchaseAsync("u1", planId);

        Assert.False(ok);
        Assert.Contains("inactive", error, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsActiveAsync_false_after_expiry()
    {
        var db = CreateDb();
        var planId = await CreatePlanAsync(db, 1);
        var service = new MembershipService(db);
        await service.PurchaseAsync("u1", planId);

        var membership = await db.Set<Membership>().SingleAsync();
        membership.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        Assert.False(await service.IsActiveAsync("u1"));
    }

    [Fact]
    public async Task GetExpiringAsync_returns_memberships_within_window()
    {
        var db = CreateDb();
        var planId = await CreatePlanAsync(db, 30);
        var service = new MembershipService(db);
        await service.PurchaseAsync("u1", planId);

        var membership = await db.Set<Membership>().SingleAsync();
        membership.ExpiresAt = DateTime.UtcNow.AddDays(3);
        await db.SaveChangesAsync();

        var expiring = await service.GetExpiringAsync(withinDays: 7);
        Assert.Contains(expiring, m => m.UserId == "u1");
    }
}
