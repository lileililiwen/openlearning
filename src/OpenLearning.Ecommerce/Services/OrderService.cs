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

    // ===== Finance admin =====

    /// <summary>Filters for the admin all-orders page.</summary>
    public sealed record OrderFilter(
        OrderStatus? Status,
        DateTime? From,
        DateTime? To,
        string? Search);

    /// <summary>
    /// Admin listing of every order with status/date/search filters, a page of
    /// results, and totals over the filtered set.
    /// </summary>
    public async Task<(List<Order> Orders, int TotalCount, decimal TotalAmount)> GetAdminOrdersAsync(
        OrderFilter filter, int page, int pageSize)
    {
        IQueryable<Order> query = _db.Set<Order>().AsNoTracking();
        if (filter.Status is not null)
        {
            query = query.Where(o => o.Status == filter.Status);
        }

        if (filter.From is not null)
        {
            query = query.Where(o => o.CreatedAt >= DateTime.SpecifyKind(filter.From.Value, DateTimeKind.Utc));
        }

        if (filter.To is not null)
        {
            var end = DateTime.SpecifyKind(filter.To.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(o => o.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(o =>
                (o.Course != null && o.Course.Title.Contains(term))
                || (o.Student != null && (o.Student.DisplayName.Contains(term) || (o.Student.Email != null && o.Student.Email.Contains(term)))));
        }

        var totalCount = await query.CountAsync();
        var totalAmount = await query.SumAsync(o => (decimal?)o.Amount) ?? 0m;
        var orders = await query
            .Include(o => o.Course)
            .Include(o => o.Student)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (orders, totalCount, totalAmount);
    }

    /// <summary>Admin approves or rejects a pending refund request.</summary>
    public async Task<(bool Ok, string? Error)> ReviewRefundAsync(int orderId, bool approve)
    {
        var order = await _db.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.RefundStatus != RefundStatus.Requested)
        {
            return (false, "This order has no pending refund request.");
        }

        order.RefundStatus = approve ? RefundStatus.Approved : RefundStatus.Rejected;
        if (approve)
        {
            order.Status = OrderStatus.Refunded;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Orders with a pending refund request (admin review queue).</summary>
    public Task<List<Order>> GetRefundRequestsAsync()
    {
        return _db.Set<Order>().AsNoTracking()
            .Where(o => o.RefundStatus == RefundStatus.Requested)
            .Include(o => o.Course)
            .Include(o => o.Student)
            .OrderBy(o => o.RefundRequestedAt)
            .ToListAsync();
    }

    /// <summary>Loads any order with course/student for the admin UI.</summary>
    public Task<Order?> GetByIdForAdminAsync(int id)
    {
        return _db.Set<Order>().AsNoTracking()
            .Include(o => o.Course)
            .Include(o => o.Student)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>Orders created in one cart checkout (for settlement crediting).</summary>
    public Task<List<Order>> GetOrdersByCheckoutIdAsync(Guid checkoutId)
    {
        return _db.Set<Order>().AsNoTracking()
            .Where(o => o.CartCheckoutId == checkoutId)
            .Include(o => o.Course)
            .ToListAsync();
    }

    /// <summary>One row of the reconciliation report.</summary>
    public sealed record ReconRow(
        int CourseId,
        string CourseTitle,
        int GrossOrders,
        decimal Gross,
        int RefundedOrders,
        decimal Refunds,
        decimal Net);

    /// <summary>
    /// Per-course and total reconciliation over a paid period: gross paid
    /// orders, refunded orders/amount, and net = gross - refunds.
    /// </summary>
    public async Task<(List<ReconRow> Rows, int TotalGrossOrders, decimal TotalGross, int TotalRefundedOrders, decimal TotalRefunds, decimal TotalNet)>
        GetReconciliationAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        DateTime? rangeEnd = null;
        if (to is not null)
        {
            rangeEnd = DateTime.SpecifyKind(to.Value.Date, DateTimeKind.Utc).AddDays(1);
        }

        IQueryable<Order> query = _db.Set<Order>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(o => o.PaidAt >= rangeFrom.Value);
        }

        if (rangeEnd is not null)
        {
            query = query.Where(o => o.PaidAt < rangeEnd.Value);
        }

        var orders = await query
            .Include(o => o.Course)
            .ToListAsync();

        var rows = orders
            .GroupBy(o => new { o.CourseId, CourseTitle = o.Course?.Title ?? string.Empty })
            .Select(g =>
            {
                var gross = g.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.Amount);
                var refunds = g.Where(o => o.Status == OrderStatus.Refunded).Sum(o => o.Amount);
                return new ReconRow(
                    g.Key.CourseId,
                    g.Key.CourseTitle,
                    g.Count(o => o.Status == OrderStatus.Paid),
                    gross,
                    g.Count(o => o.Status == OrderStatus.Refunded),
                    refunds,
                    Math.Round(gross - refunds, 2));
            })
            .OrderByDescending(r => r.Net)
            .ToList();

        var totalGross = orders.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.Amount);
        var totalRefunds = orders.Where(o => o.Status == OrderStatus.Refunded).Sum(o => o.Amount);
        return (
            rows,
            orders.Count(o => o.Status == OrderStatus.Paid),
            totalGross,
            orders.Count(o => o.Status == OrderStatus.Refunded),
            totalRefunds,
            Math.Round(totalGross - totalRefunds, 2));
    }

    /// <summary>Outcome of a cart checkout.</summary>
    public sealed record CheckoutResult(
        int OrderCount,
        decimal TotalPaid,
        decimal TotalDiscount,
        int PointsAwarded,
        Guid? CheckoutId,
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
            return new CheckoutResult(0, 0m, 0m, 0, null, "Your cart is empty.");
        }

        // Validate every item before creating any orders.
        foreach (var course in items.Select(i => i.Course))
        {
            if (course is null || course.Status != CourseStatus.Published)
            {
                return new CheckoutResult(0, 0m, 0m, 0, null, $"{course?.Title ?? "A course"} is not available for purchase.");
            }

            if (course.Price is null or <= 0)
            {
                return new CheckoutResult(0, 0m, 0m, 0, null, $"'{course.Title}' is free — enroll directly.");
            }

            if (course.InstructorId == studentId)
            {
                return new CheckoutResult(0, 0m, 0m, 0, null, "You own one of the courses in your cart.");
            }

            if (await _enrollments.IsEnrolledAsync(studentId, course.Id))
            {
                return new CheckoutResult(0, 0m, 0m, 0, null, $"You are already enrolled in '{course.Title}'.");
            }
        }

        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var (found, error) = await _coupons.ValidateAsync(couponCode, studentId);
            if (found is null)
            {
                return new CheckoutResult(0, 0m, 0m, 0, null, error);
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
                return new CheckoutResult(0, 0m, 0m, 0, null, enrollError ?? "Failed to enroll after payment.");
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
        return new CheckoutResult(items.Count, totalPaid, totalDiscount, pointsAwarded, checkoutId, null);
    }
}
