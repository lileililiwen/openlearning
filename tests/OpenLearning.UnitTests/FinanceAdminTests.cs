using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using Xunit;

namespace OpenLearning.UnitTests.Ecommerce;

public sealed class FinanceAdminTests
{
    private static async Task<(ApplicationDbContext Db, int CourseAId, int CourseBId, OrderService Orders, CouponService Coupons)>
        SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var courseA = new Course { Title = "Alpha", InstructorId = "i1", Status = CourseStatus.Published, Price = 100m };
        var courseB = new Course { Title = "Beta", InstructorId = "i2", Status = CourseStatus.Published, Price = 50m };
        db.Set<Course>().AddRange(courseA, courseB);
        await db.SaveChangesAsync();

        var alice = new ApplicationUser { Id = "s1", UserName = "alice@x.com", Email = "alice@x.com", DisplayName = "Alice" };
        var bob = new ApplicationUser { Id = "s2", UserName = "bob@x.com", Email = "bob@x.com", DisplayName = "Bob" };
        db.Set<ApplicationUser>().AddRange(alice, bob);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        db.Set<Order>().AddRange(
            new Order { CourseId = courseA.Id, Course = courseA, StudentId = "s1", Student = alice, Amount = 100m, Status = OrderStatus.Paid, PaidAt = now, PaymentReference = "DEMO-1", CreatedAt = now },
            new Order { CourseId = courseA.Id, Course = courseA, StudentId = "s1", Student = alice, Amount = 40m, Status = OrderStatus.Refunded, PaidAt = now, PaymentReference = "DEMO-2", CreatedAt = now },
            new Order { CourseId = courseB.Id, Course = courseB, StudentId = "s2", Student = bob, Amount = 50m, Status = OrderStatus.Pending, CreatedAt = now });
        await db.SaveChangesAsync();

        var enrollments = new EnrollmentService(db);
        var orders = new OrderService(db, enrollments, new CartService(db, enrollments), new CouponService(db), new LedgerService(db));
        return (db, courseA.Id, courseB.Id, orders, new CouponService(db));
    }

    [Fact]
    public async Task GetAdminOrders_filters_by_status_and_search()
    {
        var (_, _, _, orders, _) = await SeedAsync();

        var (_, totalAll, amountAll) = await orders.GetAdminOrdersAsync(
            new OrderService.OrderFilter(null, null, null, null), 1, 20);
        Assert.Equal(3, totalAll);
        Assert.Equal(190m, amountAll);

        var (_, paidTotal, _) = await orders.GetAdminOrdersAsync(
            new OrderService.OrderFilter(OrderStatus.Paid, null, null, null), 1, 20);
        Assert.Equal(1, paidTotal);

        var (_, aliceTotal, _) = await orders.GetAdminOrdersAsync(
            new OrderService.OrderFilter(null, null, null, "Alice"), 1, 20);
        Assert.Equal(2, aliceTotal);

        var (_, alphaTotal, _) = await orders.GetAdminOrdersAsync(
            new OrderService.OrderFilter(null, null, null, "Alpha"), 1, 20);
        Assert.Equal(2, alphaTotal);
    }

    [Fact]
    public async Task ReviewRefund_approve_sets_refunded_and_reject_keeps_paid()
    {
        var (db, courseAId, _, orders, _) = await SeedAsync();
        var now = DateTime.UtcNow;
        var approveOrder = new Order
        {
            CourseId = courseAId,
            StudentId = "s1",
            Amount = 100m,
            Status = OrderStatus.Paid,
            RefundStatus = RefundStatus.Requested,
            RefundRequestedAt = now,
        };
        var rejectOrder = new Order
        {
            CourseId = courseAId,
            StudentId = "s2",
            Amount = 60m,
            Status = OrderStatus.Paid,
            RefundStatus = RefundStatus.Requested,
            RefundRequestedAt = now,
        };
        db.Set<Order>().AddRange(approveOrder, rejectOrder);
        await db.SaveChangesAsync();

        Assert.True((await orders.ReviewRefundAsync(approveOrder.Id, approve: true)).Ok);
        Assert.True((await orders.ReviewRefundAsync(rejectOrder.Id, approve: false)).Ok);

        var approved = await db.Set<Order>().SingleAsync(o => o.Id == approveOrder.Id);
        var rejected = await db.Set<Order>().SingleAsync(o => o.Id == rejectOrder.Id);
        Assert.Equal(OrderStatus.Refunded, approved.Status);
        Assert.Equal(RefundStatus.Approved, approved.RefundStatus);
        Assert.Equal(OrderStatus.Paid, rejected.Status);
        Assert.Equal(RefundStatus.Rejected, rejected.RefundStatus);

        // A non-requested order cannot be reviewed.
        Assert.False((await orders.ReviewRefundAsync(approveOrder.Id, approve: true)).Ok);
    }

    [Fact]
    public async Task GetRefundRequests_returns_only_requested_orders()
    {
        var (db, courseAId, _, orders, _) = await SeedAsync();
        db.Set<Order>().Add(new Order
        {
            CourseId = courseAId,
            StudentId = "s1",
            Amount = 80m,
            Status = OrderStatus.Paid,
            RefundStatus = RefundStatus.Requested,
            RefundRequestedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var requests = await orders.GetRefundRequestsAsync();

        Assert.Single(requests);
        Assert.Equal(RefundStatus.Requested, requests[0].RefundStatus);
    }

    [Fact]
    public async Task GetReconciliation_computes_gross_refunds_and_net()
    {
        var (db, _, courseBId, orders, _) = await SeedAsync();
        db.Set<Order>().Add(new Order
        {
            CourseId = courseBId,
            StudentId = "s2",
            Amount = 50m,
            Status = OrderStatus.Paid,
            PaidAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var (rows, grossOrders, gross, refundedOrders, refunds, net) =
            await orders.GetReconciliationAsync(null, null);

        Assert.Equal(2, grossOrders);
        Assert.Equal(150m, gross);       // Alpha paid 100 + Beta paid 50
        Assert.Equal(1, refundedOrders);
        Assert.Equal(40m, refunds);      // the Alpha refunded order
        Assert.Equal(110m, net);
        Assert.Equal(2, rows.Count);     // Alpha and Beta
    }

    [Fact]
    public async Task Coupon_update_and_deactivate_work()
    {
        var (_, _, _, _, coupons) = await SeedAsync();
        Assert.True((await coupons.CreateAsync("PCT", 10, null, null, null)).Ok);
        var coupon = (await coupons.GetAllAsync()).Single();

        Assert.True((await coupons.UpdateAsync(coupon.Id, null, 5m, null, 5)).Ok);
        var updated = await coupons.GetByIdAsync(coupon.Id);
        Assert.Equal(5m, updated!.DiscountAmount);
        Assert.Equal(5, updated.MaxUses);

        // Both discount kinds invalid.
        Assert.False((await coupons.UpdateAsync(coupon.Id, 10, 5m, null, null)).Ok);

        Assert.True((await coupons.SetActiveAsync(coupon.Id, false)).Ok);
        Assert.False((await coupons.GetByIdAsync(coupon.Id))!.IsActive);
    }
}
