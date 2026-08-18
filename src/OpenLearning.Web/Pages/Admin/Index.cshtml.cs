using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class IndexModel : PageModel
{
    private readonly AccountService _accounts;
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly OrderService _orders;
    private readonly ProgressService _progress;

    public IndexModel(
        AccountService accounts,
        CourseService courses,
        EnrollmentService enrollments,
        OrderService orders,
        ProgressService progress)
    {
        _accounts = accounts;
        _courses = courses;
        _enrollments = enrollments;
        _orders = orders;
        _progress = progress;
    }

    public int StudentCount { get; set; }

    public int InstructorCount { get; set; }

    public int DraftCourseCount { get; set; }

    public int PublishedCourseCount { get; set; }

    public int EnrollmentCount { get; set; }

    public decimal PaidRevenue { get; set; }

    public int? CompletionRate { get; set; }

    public List<ApplicationUser> RecentSignups { get; set; } = new();

    public List<Course> RecentCourses { get; set; } = new();

    public List<Order> RecentOrders { get; set; } = new();

    public async Task OnGetAsync()
    {
        StudentCount = await _accounts.CountUsersInRoleAsync(Roles.Student);
        InstructorCount = await _accounts.CountUsersInRoleAsync(Roles.Instructor);

        var (draft, published) = await _courses.GetCourseCountsAsync();
        DraftCourseCount = draft;
        PublishedCourseCount = published;

        EnrollmentCount = await _enrollments.GetTotalEnrollmentsAsync();
        PaidRevenue = await _orders.GetTotalPaidRevenueAsync();
        CompletionRate = await _progress.GetPlatformCompletionRateAsync();

        RecentSignups = await _accounts.GetRecentSignupsAsync(5);
        RecentCourses = await _courses.GetRecentCoursesAsync(5);
        RecentOrders = await _orders.GetRecentOrdersAsync(5);
    }
}
