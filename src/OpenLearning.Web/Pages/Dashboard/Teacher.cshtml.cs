using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Web.Pages.Dashboard;

/// <summary>Per-course teaching statistics shown on the teacher dashboard.</summary>
public sealed record TeacherCourseItem(
    int CourseId,
    string CourseTitle,
    string Category,
    CourseStatus Status,
    bool IsFree,
    int EnrollmentCount,
    decimal PaidRevenue,
    int? CompletionRate,
    int? QuizPassRate);

[Authorize(Policy = Policies.RequireInstructor)]
public class TeacherModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly OrderService _orders;
    private readonly ProgressService _progress;
    private readonly AttemptService _attempts;

    public TeacherModel(
        CourseService courses,
        EnrollmentService enrollments,
        OrderService orders,
        ProgressService progress,
        AttemptService attempts)
    {
        _courses = courses;
        _enrollments = enrollments;
        _orders = orders;
        _progress = progress;
        _attempts = attempts;
    }

    public List<TeacherCourseItem> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var courses = await _courses.GetByInstructorAsync(userId);
        foreach (var course in courses)
        {
            Courses.Add(new TeacherCourseItem(
                course.Id,
                course.Title,
                course.Category,
                course.Status,
                course.IsFree,
                await _enrollments.GetEnrollmentCountAsync(course.Id),
                await _orders.GetPaidRevenueForCourseAsync(course.Id),
                await _progress.GetCourseCompletionRateAsync(course.Id),
                await _attempts.GetCourseQuizPassRateAsync(course.Id)));
        }
    }
}
