using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Ecommerce.Services;

public class CartService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;

    public CartService(DbContext db, EnrollmentService enrollments)
    {
        _db = db;
        _enrollments = enrollments;
    }

    public Task<List<CartItem>> GetItemsAsync(string studentId)
    {
        return _db.Set<CartItem>().AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .Include(c => c.Course)
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync();
    }

    public Task<int> GetCountAsync(string studentId)
    {
        return _db.Set<CartItem>().CountAsync(c => c.StudentId == studentId);
    }

    public async Task<(bool Ok, string? Error)> AddAsync(string studentId, int courseId)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (false, "Course not found.");
        }

        if (course.Status != CourseStatus.Published)
        {
            return (false, "This course is not available for purchase.");
        }

        if (course.Price is null or <= 0)
        {
            return (false, "This course is free — enroll directly.");
        }

        if (course.InstructorId == studentId)
        {
            return (false, "You own this course.");
        }

        if (await _enrollments.IsEnrolledAsync(studentId, courseId))
        {
            return (false, "You are already enrolled in this course.");
        }

        var alreadyInCart = await _db.Set<CartItem>()
            .AnyAsync(c => c.StudentId == studentId && c.CourseId == courseId);
        if (alreadyInCart)
        {
            return (false, "This course is already in your cart.");
        }

        _db.Set<CartItem>().Add(new CartItem { StudentId = studentId, CourseId = courseId });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveAsync(string studentId, int courseId)
    {
        var item = await _db.Set<CartItem>()
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId);
        if (item is null)
        {
            return false;
        }

        _db.Set<CartItem>().Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ClearAsync(string studentId)
    {
        var items = await _db.Set<CartItem>()
            .Where(c => c.StudentId == studentId)
            .ToListAsync();
        _db.Set<CartItem>().RemoveRange(items);
        await _db.SaveChangesAsync();
    }
}
