using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
public class OrdersModel : PageModel
{
    private readonly CourseService _courses;
    private readonly OrderService _orders;

    public OrdersModel(CourseService courses, OrderService orders)
    {
        _courses = courses;
        _orders = orders;
    }

    public Course? Course { get; set; }

    public List<Order> Orders { get; set; } = new();

    public decimal TotalRevenue { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        if (course.InstructorId != userId)
        {
            return Forbid();
        }

        Course = course;
        Orders = await _orders.GetOrdersForCourseAsync(id, userId);
        TotalRevenue = Orders.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.Amount);
        return Page();
    }
}
