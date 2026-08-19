using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Ecommerce.Services;

public class OrderService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;
    private readonly CartService _cart;
    private readonly CouponService _coupons;
    private readonly LedgerService _ledger;

    public OrderService(
        DbContext db,
        EnrollmentService enrollments,
        CartService cart,
        CouponService coupons,
        LedgerService ledger)
    {
        _db = db;
        _enrollments = enrollments;
        _cart = cart;
        _coupons = coupons;
        _ledger = ledger;
    }

    public async Task<(Order? Order, string? Error)> CreateAsync(string studentId, int courseId)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (null, "Course not found.");
        }

        if (course.Status != CourseStatus.Published)
        {
            return (null, "This course is not available for purchase.");
        }

        if (course.Price is null or <= 0)
        {
            return (null, "This course is free — enroll directly.");
        }

        if (course.InstructorId == studentId)
        {
            return (null, "You own this course.");
        }

        var alreadyEnrolled = await _enrollments.IsEnrolledAsync(studentId, courseId);
        if (alreadyEnrolled)
        {
            return (null, "You are already enrolled in this course.");
        }

        var hasPending = await _db.Set<Order>().AnyAsync(o =>
            o.StudentId == studentId && o.CourseId == courseId && o.Status == OrderStatus.Pending);
        if (hasPending)
        {
            return (null, "You already have a pending order for this course.");
        }

        var order = new Order
        {
            CourseId = courseId,
            StudentId = studentId,
            Amount = course.Price.Value,
        };

        _db.Set<Order>().Add(order);
        await _db.SaveChangesAsync();
        return (order, null);
    }

    public async Task<(bool Ok, string? Error)> ConfirmPaymentAsync(int orderId, string studentId)
    {
        var order = await _db.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StudentId == studentId);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.Status == OrderStatus.Paid)
        {
            return (true, null);
        }

        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;
        order.PaymentReference = $"DEMO-{order.Id:D6}";
        await _db.SaveChangesAsync();

        var (ok, error) = await _enrollments.EnrollAsync(studentId, order.CourseId);
        if (!ok)
        {
            return (false, error ?? "Failed to enroll after payment.");
        }

        return (true, null);
    }

    public Task<bool> HasPaidOrderAsync(string studentId, int courseId)
    {
        return _db.Set<Order>().AnyAsync(o =>
                o.StudentId == studentId && o.CourseId == courseId && o.Status == OrderStatus.Paid);
    }

    public Task<Order?> GetPendingOrderAsync(string studentId, int courseId)
    {
        return _db.Set<Order>().AsNoTracking()
                .Include(o => o.Course)
                .FirstOrDefaultAsync(o =>
                    o.StudentId == studentId && o.CourseId == courseId && o.Status == OrderStatus.Pending);
    }

    public Task<Order?> GetByIdAsync(int orderId, string studentId)
    {
        return _db.Set<Order>().AsNoTracking()
                .Include(o => o.Course)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.StudentId == studentId);
    }

    public Task<List<Order>> GetOrdersForCourseAsync(int courseId, string ownerId)
    {
        return _db.Set<Order>().AsNoTracking()
                .Where(o => o.CourseId == courseId && o.Course!.InstructorId == ownerId)
                .Include(o => o.Student)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
    }

    public async Task<decimal> GetPaidRevenueForCourseAsync(int courseId)
    {
        var sum = await _db.Set<Order>()
            .Where(o => o.CourseId == courseId && o.Status == OrderStatus.Paid)
            .SumAsync(o => (decimal?)o.Amount);
        return sum ?? 0m;
    }

    public async Task<decimal> GetTotalPaidRevenueAsync()
    {
        var sum = await _db.Set<Order>()
            .Where(o => o.Status == OrderStatus.Paid)
            .SumAsync(o => (decimal?)o.Amount);
        return sum ?? 0m;
    }

    public Task<List<Order>> GetRecentOrdersAsync(int count)
    {
        return _db.Set<Order>().AsNoTracking()
                .Where(o => o.Status == OrderStatus.Paid)
                .Include(o => o.Course)
                .Include(o => o.Student)
                .OrderByDescending(o => o.PaidAt ?? o.CreatedAt)
                .Take(count)
                .ToListAsync();
    }

    // ===== Platform analytics (admin reports) =====

    /// <summary>One row of the revenue report: paid revenue per course.</summary>
    public sealed record RevenueByCourseRow(
        int CourseId,
        string CourseTitle,
        string InstructorName,
        int OrderCount,
        decimal Revenue);

    /// <summary>
    /// Paid orders in the given range grouped by course, joined to the
    /// instructor, ordered by revenue. Null dates mean all time.
    /// </summary>
    public async Task<(List<RevenueByCourseRow> Rows, decimal TotalRevenue, int TotalOrders)>
        GetRevenueReportAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<Order> query = _db.Set<Order>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid);
        if (rangeFrom is not null)
        {
            query = query.Where(o => o.PaidAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(o => o.PaidAt < end);
        }

        var orders = await query
            .Include(o => o.Course)!.ThenInclude(c => c!.Instructor)
            .ToListAsync();

        var rows = orders
            .GroupBy(o => new { o.CourseId, CourseTitle = o.Course?.Title ?? string.Empty, InstructorName = o.Course?.Instructor?.DisplayName ?? string.Empty })
            .Select(g => new RevenueByCourseRow(
                g.Key.CourseId,
                g.Key.CourseTitle,
                g.Key.InstructorName,
                g.Count(),
                g.Sum(o => o.Amount)))
            .OrderByDescending(r => r.Revenue)
            .ToList();

        var totalRevenue = await query.SumAsync(o => (decimal?)o.Amount) ?? 0m;
        var totalOrders = await query.CountAsync();
        return (rows, totalRevenue, totalOrders);
    }

    /// <summary>Paid orders in the range with course/student, for CSV export.</summary>
    public async Task<List<Order>> GetPaidOrdersForExportAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<Order> query = _db.Set<Order>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid);
        if (rangeFrom is not null)
        {
            query = query.Where(o => o.PaidAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(o => o.PaidAt < end);
        }

        return await query
            .Include(o => o.Course)
            .Include(o => o.Student)
            .OrderByDescending(o => o.PaidAt)
            .ToListAsync();
    }

    /// <summary>
    /// Date-only inputs bind with Kind=Unspecified, which Npgsql rejects for
    /// timestamptz columns. Treat an Unspecified value as UTC.
    /// </summary>
    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value is null
                ? null
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    // ===== Commerce extras =====

    /// <summary>All orders for a student, newest first.</summary>
    public Task<List<Order>> GetOrdersForStudentAsync(string studentId)
    {
        return _db.Set<Order>().AsNoTracking()
            .Where(o => o.StudentId == studentId)
            .Include(o => o.Course)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Marks a paid order as refund-requested.</summary>
    public async Task<(bool Ok, string? Error)> RequestRefundAsync(int orderId, string studentId)
    {
        var order = await _db.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StudentId == studentId);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.Status != OrderStatus.Paid)
        {
            return (false, "Only paid orders can be refunded.");
        }

        if (order.RefundStatus != RefundStatus.None)
        {
            return (false, "A refund was already requested for this order.");
        }

        order.RefundStatus = RefundStatus.Requested;
        order.RefundRequestedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Outcome of a cart checkout.</summary>
    public sealed record CheckoutResult(
        int OrderCount,
        decimal TotalPaid,
        decimal TotalDiscount,
        int PointsAwarded,
        string? Error);

    /// <summary>
    /// Checks out every cart item, creating and confirming one paid order per
    /// course. A coupon (percent or amount) applies first, then account
    /// balance, then loyalty points (100 points = $1.00). Ledgers and the
    /// coupon redemption are recorded; the cart is cleared.
    /// </summary>
    public async Task<CheckoutResult> CheckoutCartAsync(
        string studentId, string? couponCode, bool useBalance, bool usePoints)
    {
        var items = await _db.Set<CartItem>().AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .Include(c => c.Course)
            .ToListAsync();
        if (items.Count == 0)
        {
            return new CheckoutResult(0, 0m, 0m, 0, "Your cart is empty.");
        }

        // Validate every item before creating any orders.
        foreach (var course in items.Select(i => i.Course))
        {
            if (course is null || course.Status != CourseStatus.Published)
            {
                return new CheckoutResult(0, 0m, 0m, 0, $"{course?.Title ?? "A course"} is not available for purchase.");
            }

            if (course.Price is null or <= 0)
            {
                return new CheckoutResult(0, 0m, 0m, 0, $"'{course.Title}' is free — enroll directly.");
            }

            if (course.InstructorId == studentId)
            {
                return new CheckoutResult(0, 0m, 0m, 0, "You own one of the courses in your cart.");
            }

            if (await _enrollments.IsEnrolledAsync(studentId, course.Id))
            {
                return new CheckoutResult(0, 0m, 0m, 0, $"You are already enrolled in '{course.Title}'.");
            }
        }

        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var (found, error) = await _coupons.ValidateAsync(couponCode, studentId);
            if (found is null)
            {
                return new CheckoutResult(0, 0m, 0m, 0, error);
            }

            coupon = found;
        }

        var balanceRemaining = useBalance ? await _ledger.GetBalanceAsync(studentId) : 0m;
        var pointsRemaining = usePoints ? await _ledger.GetPointsAsync(studentId) : 0;

        var checkoutId = Guid.NewGuid();
        var totalPaid = 0m;
        var totalDiscount = 0m;
        var balanceUsedTotal = 0m;
        var pointsUsedTotal = 0;
        var pointsAwarded = 0;
        Order? firstOrder = null;

        foreach (var item in items)
        {
            var price = item.Course!.Price!.Value;
            var discount = coupon is null ? 0m : CouponService.ComputeDiscount(coupon, price);
            var payable = Math.Round(price - discount, 2);

            var balanceUsed = 0m;
            if (balanceRemaining > 0 && payable > 0)
            {
                balanceUsed = Math.Min(balanceRemaining, payable);
                payable = Math.Round(payable - balanceUsed, 2);
                balanceRemaining -= balanceUsed;
                balanceUsedTotal += balanceUsed;
            }

            var pointsUsed = 0;
            if (pointsRemaining > 0 && payable > 0)
            {
                pointsUsed = Math.Min(pointsRemaining, (int)Math.Ceiling(payable * 100m));
                var pointsValue = Math.Round(pointsUsed / 100m, 2);
                payable = Math.Round(payable - pointsValue, 2);
                pointsRemaining -= pointsUsed;
                pointsUsedTotal += pointsUsed;
            }

            var order = new Order
            {
                CourseId = item.CourseId,
                StudentId = studentId,
                Amount = Math.Max(0m, payable),
                CouponId = coupon?.Id,
                DiscountAmount = discount,
                PaidWithBalance = balanceUsed,
                CartCheckoutId = checkoutId,
            };
            _db.Set<Order>().Add(order);
            await _db.SaveChangesAsync();

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
            order.PaymentReference = $"DEMO-{order.Id:D6}";
            await _db.SaveChangesAsync();

            var (enrolled, enrollError) = await _enrollments.EnrollAsync(studentId, order.CourseId);
            if (!enrolled)
            {
                return new CheckoutResult(0, 0m, 0m, 0, enrollError ?? "Failed to enroll after payment.");
            }

            totalPaid += order.Amount;
            totalDiscount += discount;
            pointsAwarded += (int)Math.Floor(order.Amount);
            firstOrder ??= order;
        }

        if (coupon is not null && firstOrder is not null)
        {
            await _coupons.RedeemAsync(coupon.Id, studentId, firstOrder.Id);
        }

        if (balanceUsedTotal > 0)
        {
            await _ledger.AddBalanceAsync(studentId, -balanceUsedTotal, $"Cart checkout {checkoutId:N}");
        }

        if (pointsUsedTotal > 0)
        {
            await _ledger.AddPointsAsync(studentId, -pointsUsedTotal, $"Cart checkout {checkoutId:N}");
        }

        if (pointsAwarded > 0)
        {
            await _ledger.AddPointsAsync(studentId, pointsAwarded, "Purchase reward");
        }

        await _cart.ClearAsync(studentId);
        return new CheckoutResult(items.Count, totalPaid, totalDiscount, pointsAwarded, null);
    }
}
