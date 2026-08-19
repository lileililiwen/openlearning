using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using Xunit;

namespace OpenLearning.UnitTests.Ecommerce;

public sealed class CommerceExtrasTests
{
    private static async Task<(ApplicationDbContext Db, int PaidCourseId, int FreeCourseId, string StudentId)> Seed()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var paid = new Course { Title = "Paid", InstructorId = "i1", Status = CourseStatus.Published, Price = 100m };
        var free = new Course { Title = "Free", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().AddRange(paid, free);
        await db.SaveChangesAsync();
        return (db, paid.Id, free.Id, "s1");
    }

    private static OrderService Orders(ApplicationDbContext db)
    {
        var enrollments = new EnrollmentService(db);
        return new OrderService(db, enrollments, new CartService(db, enrollments), new CouponService(db), new LedgerService(db));
    }

    [Fact]
    public async Task Cart_add_remove_and_count_validate_course()
    {
        var (db, paidId, freeId, studentId) = await Seed();
        var cart = new CartService(db, new EnrollmentService(db));

        var (freeOk, freeError) = await cart.AddAsync(studentId, freeId);
        Assert.False(freeOk);
        Assert.NotNull(freeError);

        Assert.True((await cart.AddAsync(studentId, paidId)).Ok);
        Assert.False((await cart.AddAsync(studentId, paidId)).Ok); // duplicate

        Assert.Equal(1, await cart.GetCountAsync(studentId));
        Assert.Single(await cart.GetItemsAsync(studentId));

        Assert.True(await cart.RemoveAsync(studentId, paidId));
        Assert.Equal(0, await cart.GetCountAsync(studentId));
    }

    [Fact]
    public async Task Coupon_validate_and_redeem_enforce_usage_limits()
    {
        var (db, _, _, studentId) = await Seed();
        var coupons = new CouponService(db);
        Assert.True((await coupons.CreateAsync("SAVE10", 10, null, null, null)).Ok);

        var (found, _) = await coupons.ValidateAsync("save10", studentId);
        Assert.NotNull(found);

        Assert.True((await coupons.RedeemAsync(found.Id, studentId, 1)).Ok);

        var (usedAgain, error) = await coupons.ValidateAsync("SAVE10", studentId);
        Assert.Null(usedAgain);
        Assert.Contains("already", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Coupon_create_requires_exactly_one_discount_kind()
    {
        var (db, _, _, _) = await Seed();
        var coupons = new CouponService(db);

        Assert.False((await coupons.CreateAsync("BOTH", 10, 5m, null, null)).Ok);
        Assert.False((await coupons.CreateAsync("NONE", null, null, null, null)).Ok);
        Assert.False((await coupons.CreateAsync("BADPCT", 150, null, null, null)).Ok);
        Assert.True((await coupons.CreateAsync("AMT5", null, 5m, null, null)).Ok);
        Assert.False((await coupons.CreateAsync("AMT5", null, 5m, null, null)).Ok); // duplicate code
    }

    [Fact]
    public async Task Ledger_balance_and_points_are_running_sums()
    {
        var (db, _, _, studentId) = await Seed();
        var ledger = new LedgerService(db);

        await ledger.AddBalanceAsync(studentId, 50m, "bonus");
        await ledger.AddBalanceAsync(studentId, -10m, "spend");
        Assert.Equal(40m, await ledger.GetBalanceAsync(studentId));

        await ledger.AddPointsAsync(studentId, 100, "earn");
        Assert.Equal(100, await ledger.GetPointsAsync(studentId));
    }

    [Fact]
    public async Task Invoice_request_requires_paid_order_and_is_single_use()
    {
        var (db, paidId, _, studentId) = await Seed();
        var order = new Order { CourseId = paidId, StudentId = studentId, Amount = 100m };
        db.Set<Order>().Add(order);
        await db.SaveChangesAsync();
        var invoices = new InvoiceService(db);

        Assert.False((await invoices.RequestAsync(order.Id, studentId)).Ok); // not paid

        order.Status = OrderStatus.Paid;
        await db.SaveChangesAsync();
        Assert.True((await invoices.RequestAsync(order.Id, studentId)).Ok);
        Assert.False((await invoices.RequestAsync(order.Id, studentId)).Ok); // already requested
    }

    [Fact]
    public async Task Checkout_creates_paid_order_applies_coupon_balance_and_points()
    {
        var (db, paidId, _, studentId) = await Seed();
        await new LedgerService(db).AddBalanceAsync(studentId, 50m, "bonus");
        await new LedgerService(db).AddPointsAsync(studentId, 500, "welcome");
        await new CouponService(db).CreateAsync("SAVE10", 10, null, null, null);
        var cart = new CartService(db, new EnrollmentService(db));
        await cart.AddAsync(studentId, paidId);

        var result = await Orders(db).CheckoutCartAsync(studentId, "SAVE10", useBalance: true, usePoints: true);

        Assert.Null(result.Error);
        Assert.Equal(1, result.OrderCount);
        // 100 - 10% coupon (10) = 90; - balance (50) = 40; - 500 pts ($5) = 35
        Assert.Equal(35m, result.TotalPaid);
        Assert.Equal(10m, result.TotalDiscount);

        var order = await db.Set<Order>().SingleAsync();
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(35m, order.Amount);
        Assert.Equal(10m, order.DiscountAmount);
        Assert.Equal(50m, order.PaidWithBalance);

        Assert.Equal(0, await cart.GetCountAsync(studentId));
        Assert.Single(db.Set<CouponRedemption>());
        Assert.Equal(1, (await db.Set<Coupon>().SingleAsync()).Uses);
        Assert.Equal(0m, await new LedgerService(db).GetBalanceAsync(studentId));
        // 500 spent, floor(35) = 35 earned
        Assert.Equal(35, await new LedgerService(db).GetPointsAsync(studentId));
    }

    [Fact]
    public async Task Checkout_rejects_invalid_coupon_without_creating_orders()
    {
        var (db, paidId, _, studentId) = await Seed();
        var cart = new CartService(db, new EnrollmentService(db));
        await cart.AddAsync(studentId, paidId);

        var result = await Orders(db).CheckoutCartAsync(studentId, "NOPE", useBalance: false, usePoints: false);

        Assert.NotNull(result.Error);
        Assert.Empty(db.Set<Order>());
        Assert.Equal(1, await cart.GetCountAsync(studentId));
    }

    [Fact]
    public async Task RequestRefund_marks_paid_orders_only_once()
    {
        var (db, paidId, _, studentId) = await Seed();
        var order = new Order { CourseId = paidId, StudentId = studentId, Amount = 100m, Status = OrderStatus.Pending };
        db.Set<Order>().Add(order);
        await db.SaveChangesAsync();
        var orders = Orders(db);

        Assert.False((await orders.RequestRefundAsync(order.Id, studentId)).Ok);

        order.Status = OrderStatus.Paid;
        await db.SaveChangesAsync();
        Assert.True((await orders.RequestRefundAsync(order.Id, studentId)).Ok);
        Assert.False((await orders.RequestRefundAsync(order.Id, studentId)).Ok);

        var stored = await db.Set<Order>().SingleAsync();
        Assert.Equal(RefundStatus.Requested, stored.RefundStatus);
        Assert.NotNull(stored.RefundRequestedAt);
    }
}
