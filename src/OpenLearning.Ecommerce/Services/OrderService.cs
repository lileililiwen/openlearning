using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Ecommerce.Services;

public class OrderService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;

    public OrderService(DbContext db, EnrollmentService enrollments)
    {
        _db = db;
        _enrollments = enrollments;
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
        => _db.Set<Order>().AnyAsync(o =>
            o.StudentId == studentId && o.CourseId == courseId && o.Status == OrderStatus.Paid);

    public Task<Order?> GetPendingOrderAsync(string studentId, int courseId)
        => _db.Set<Order>().AsNoTracking()
            .Include(o => o.Course)
            .FirstOrDefaultAsync(o =>
                o.StudentId == studentId && o.CourseId == courseId && o.Status == OrderStatus.Pending);

    public Task<Order?> GetByIdAsync(int orderId, string studentId)
        => _db.Set<Order>().AsNoTracking()
            .Include(o => o.Course)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StudentId == studentId);

    public Task<List<Order>> GetOrdersForCourseAsync(int courseId, string ownerId)
        => _db.Set<Order>().AsNoTracking()
            .Where(o => o.CourseId == courseId && o.Course!.InstructorId == ownerId)
            .Include(o => o.Student)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

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
        => _db.Set<Order>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid)
            .Include(o => o.Course)
            .Include(o => o.Student)
            .OrderByDescending(o => o.PaidAt ?? o.CreatedAt)
            .Take(count)
            .ToListAsync();
}
