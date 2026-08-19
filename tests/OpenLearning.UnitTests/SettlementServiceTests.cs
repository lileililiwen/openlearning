using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Settlement.Models;
using OpenLearning.Settlement.Services;
using Xunit;

namespace OpenLearning.UnitTests.Settlement;

public sealed class SettlementServiceTests
{
    private static async Task<(ApplicationDbContext Db, int CourseId)> SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "Alpha", InstructorId = "i1", Status = CourseStatus.Published, Price = 100m };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        return (db, course.Id);
    }

    [Fact]
    public async Task Credit_and_available_balance_reserve_pending_withdrawals()
    {
        var (db, courseId) = await SeedAsync();
        var service = new SettlementService(db);
        await service.CreditAsync("i1", courseId, 100m, "Order #1");
        await service.CreditAsync("i1", courseId, 50m, "Order #2");

        Assert.Equal(150m, await service.GetTotalAsync("i1"));
        Assert.Equal(150m, await service.GetAvailableAsync("i1"));

        Assert.True((await service.RequestWithdrawalAsync("i1", 40m)).Ok);
        Assert.Equal(110m, await service.GetAvailableAsync("i1"));

        // Marking paid keeps the amount reserved; rejecting releases it.
        var withdrawal = (await service.GetWithdrawalsAsync("i1")).Single();
        Assert.True((await service.ReviewAsync(withdrawal.Id, approve: true, "admin")).Ok);
        Assert.Equal(110m, await service.GetAvailableAsync("i1"));
    }

    [Fact]
    public async Task RequestWithdrawal_enforces_minimum_and_available()
    {
        var (db, courseId) = await SeedAsync();
        var service = new SettlementService(db);

        Assert.False((await service.RequestWithdrawalAsync("i1", 5m)).Ok); // no balance

        await service.CreditAsync("i1", courseId, 20m, "Order #1");
        Assert.False((await service.RequestWithdrawalAsync("i1", 5m)).Ok); // below minimum
        Assert.True((await service.RequestWithdrawalAsync("i1", 10m)).Ok); // equals available
        Assert.False((await service.RequestWithdrawalAsync("i1", 11m)).Ok); // exceeds available
        Assert.False((await service.RequestWithdrawalAsync("i1", 0m)).Ok); // non-positive
    }

    [Fact]
    public async Task Review_reject_releases_reserved_balance_and_blocks_double_review()
    {
        var (db, courseId) = await SeedAsync();
        var service = new SettlementService(db);
        await service.CreditAsync("i1", courseId, 100m, "Order #1");
        Assert.True((await service.RequestWithdrawalAsync("i1", 30m)).Ok);
        Assert.True((await service.RequestWithdrawalAsync("i1", 20m)).Ok);

        var w30 = (await service.GetWithdrawalsAsync("i1")).Single(w => w.Amount == 30m);
        var w20 = (await service.GetWithdrawalsAsync("i1")).Single(w => w.Amount == 20m);

        await service.ReviewAsync(w30.Id, approve: true, "admin"); // paid
        Assert.Equal(50m, await service.GetAvailableAsync("i1"));  // 100 - 30 - 20

        await service.ReviewAsync(w20.Id, approve: false, "admin"); // rejected
        Assert.Equal(70m, await service.GetAvailableAsync("i1"));  // 100 - 30

        Assert.False((await service.ReviewAsync(w30.Id, approve: true, "admin")).Ok); // already reviewed

        var reviewed = await service.GetByIdAsync(w30.Id);
        Assert.Equal(WithdrawalStatus.Paid, reviewed!.Status);
        Assert.Equal("admin", reviewed.ReviewedBy);
    }

    [Fact]
    public async Task PerCourse_and_PerPeriod_group_ledger()
    {
        var (db, courseId) = await SeedAsync();
        var courseB = new Course { Title = "Beta", InstructorId = "i1", Status = CourseStatus.Published, Price = 50m };
        db.Set<Course>().Add(courseB);
        await db.SaveChangesAsync();
        var service = new SettlementService(db);
        await service.CreditAsync("i1", courseId, 100m, "Order #1");
        await service.CreditAsync("i1", courseB.Id, 50m, "Order #2");
        await service.CreditAsync("i1", courseId, -20m, "Refund order #3");

        var perCourse = await service.GetPerCourseAsync("i1");
        Assert.Equal(80m, perCourse.Single(p => p.CourseId == courseId).Amount);
        Assert.Equal(50m, perCourse.Single(p => p.CourseId == courseB.Id).Amount);

        var perPeriod = await service.GetPerPeriodAsync("i1");
        Assert.Single(perPeriod);
        Assert.Equal(130m, perPeriod[0].Amount);
    }
}
