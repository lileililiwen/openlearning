using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;
using OpenLearning.Ecommerce.Models;
using Xunit;

namespace OpenLearning.UnitTests.Distribution;

public sealed class DistributionServiceTests
{
    private static (ApplicationDbContext Db, DistributionService Service, Course Course) SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i", Status = CourseStatus.Published, Price = 100m };
        db.Set<Course>().Add(course);
        db.SaveChanges();
        return (db, new DistributionService(db), course);
    }

    [Fact]
    public async Task CreateLink_generates_unique_slug_and_reuses_existing()
    {
        var (db, service, course) = SeedAsync();

        var (first, error) = await service.CreateLinkAsync("dist-1", course.Id);
        var (second, _) = await service.CreateLinkAsync("dist-1", course.Id);

        Assert.Null(error);
        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.False(string.IsNullOrWhiteSpace(first.Slug));
        Assert.Single(db.Set<AffiliateLink>());
    }

    [Fact]
    public async Task RecordPaid_attributes_within_window_and_is_idempotent()
    {
        var (db, service, course) = SeedAsync();
        var link = (await service.CreateLinkAsync("dist-1", course.Id)).Link!;
        await service.RecordClickAsync(link.Id, "anon-1", null, null);
        var order = new Order { CourseId = course.Id, StudentId = "s1", Amount = 100m, Status = OrderStatus.Paid };
        db.Set<Order>().Add(order);
        await db.SaveChangesAsync();

        await service.RecordPaidAsync(order.Id, "anon-1");
        await service.RecordPaidAsync(order.Id, "anon-1");

        var attribution = Assert.Single(db.Set<Attribution>());
        Assert.Equal("dist-1", attribution.DistributorUserId);
        var commission = Assert.Single(db.Set<CommissionEntry>());
        Assert.Equal(10m, commission.Amount);
        Assert.Equal(CommissionStatus.Pending, commission.Status);
    }

    [Fact]
    public async Task RecordPaid_ignores_no_click_and_expired_window()
    {
        var (db, service, course) = SeedAsync();
        var link = (await service.CreateLinkAsync("dist-1", course.Id)).Link!;
        await service.RecordClickAsync(link.Id, "anon-old", null, null);
        var click = await db.Set<AffiliateClick>().SingleAsync();
        click.ClickedAt = DateTime.UtcNow.AddDays(-31);
        await db.SaveChangesAsync();

        var order = new Order { CourseId = course.Id, StudentId = "s1", Amount = 100m, Status = OrderStatus.Paid };
        db.Set<Order>().Add(order);
        await db.SaveChangesAsync();

        await service.RecordPaidAsync(order.Id, "anon-old");
        await service.RecordPaidAsync(order.Id, "no-click");

        Assert.Empty(db.Set<Attribution>());
        Assert.Empty(db.Set<CommissionEntry>());
    }

    [Fact]
    public async Task ReverseForOrder_reverses_pending_and_clawbacks_paid()
    {
        var (db, service, course) = SeedAsync();
        var link = (await service.CreateLinkAsync("dist-1", course.Id)).Link!;
        await service.RecordClickAsync(link.Id, "anon-1", null, null);
        var order = new Order { CourseId = course.Id, StudentId = "s1", Amount = 100m, Status = OrderStatus.Paid };
        db.Set<Order>().Add(order);
        await db.SaveChangesAsync();
        await service.RecordPaidAsync(order.Id, "anon-1");

        await service.ReverseForOrderAsync(order.Id);
        Assert.Equal(CommissionStatus.Reversed, (await db.Set<CommissionEntry>().SingleAsync()).Status);

        // Paid commission produces a clawback instead.
        var order2 = new Order { CourseId = course.Id, StudentId = "s2", Amount = 100m, Status = OrderStatus.Paid };
        db.Set<Order>().Add(order2);
        await db.SaveChangesAsync();
        await service.RecordPaidAsync(order2.Id, "anon-1");
        var entry2 = await db.Set<CommissionEntry>().SingleAsync(c => c.OrderId == order2.Id);
        entry2.Status = CommissionStatus.Paid;
        await db.SaveChangesAsync();

        await service.ReverseForOrderAsync(order2.Id);
        var clawback = Assert.Single(db.Set<CommissionEntry>(), c => c.Amount < 0);
        Assert.Equal(CommissionStatus.Available, clawback.Status);
        Assert.Equal(-10m, clawback.Amount);
    }

    [Fact]
    public async Task TransitionHeld_moves_pending_to_available()
    {
        var (db, service, _) = SeedAsync();
        db.Set<CommissionEntry>().AddRange(
            new CommissionEntry { DistributorUserId = "d1", OrderId = 1, Amount = 10m, CreatedAt = DateTime.UtcNow.AddDays(-8) },
            new CommissionEntry { DistributorUserId = "d1", OrderId = 2, Amount = 10m, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var moved = await service.TransitionHeldAsync(TimeSpan.FromDays(7));

        Assert.Equal(1, moved);
        Assert.Equal(CommissionStatus.Available,
            (await db.Set<CommissionEntry>().SingleAsync(c => c.OrderId == 1)).Status);
        Assert.Equal(CommissionStatus.Pending,
            (await db.Set<CommissionEntry>().SingleAsync(c => c.OrderId == 2)).Status);
    }

    [Fact]
    public async Task Payout_request_approve_reject()
    {
        var (db, service, _) = SeedAsync();
        db.Set<CommissionEntry>().Add(new CommissionEntry
        {
            DistributorUserId = "d1",
            OrderId = 1,
            Amount = 20m,
            Status = CommissionStatus.Available,
        });
        await db.SaveChangesAsync();

        var overLimit = await service.RequestPayoutAsync("d1", 30m);
        Assert.False(overLimit.Ok);

        var ok = await service.RequestPayoutAsync("d1", 15m);
        Assert.True(ok.Ok);
        var payout = Assert.Single(db.Set<PayoutRequest>());
        Assert.Equal(PayoutStatus.Pending, payout.Status);

        Assert.True((await service.ApprovePayoutAsync(payout.Id)).Ok);
        Assert.Equal(PayoutStatus.Approved, (await db.Set<PayoutRequest>().FindAsync(payout.Id))!.Status);
        Assert.Equal(CommissionStatus.Paid, (await db.Set<CommissionEntry>().SingleAsync()).Status);
        Assert.Equal(payout.Id, (await db.Set<CommissionEntry>().SingleAsync()).PayoutRequestId);
    }

    [Fact]
    public async Task ClosePeriod_is_idempotent()
    {
        var (db, service, _) = SeedAsync();
        var start = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        db.Set<CommissionEntry>().AddRange(
            new CommissionEntry { DistributorUserId = "d1", OrderId = 1, Amount = 10m, Status = CommissionStatus.Available, CreatedAt = start.AddDays(1) },
            new CommissionEntry { DistributorUserId = "d1", OrderId = 2, Amount = 5m, Status = CommissionStatus.Paid, CreatedAt = start.AddDays(2) });
        await db.SaveChangesAsync();

        var created = await service.ClosePeriodAsync(start, start.AddDays(7));
        var again = await service.ClosePeriodAsync(start, start.AddDays(7));

        Assert.Equal(1, created);
        Assert.Equal(0, again);
        var statement = Assert.Single(db.Set<DistributorSettlementStatement>());
        Assert.Equal(15m, statement.TotalAmount);
    }
}
