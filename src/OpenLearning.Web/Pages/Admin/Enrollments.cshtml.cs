using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Logging.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "学员管理")]
public class EnrollmentsModel : PageModel
{
    private readonly EnrollmentService _enrollments;
    private readonly CourseService _courses;
    private readonly LogService _logs;

    public EnrollmentsModel(EnrollmentService enrollments, CourseService courses, LogService logs)
    {
        _enrollments = enrollments;
        _courses = courses;
        _logs = logs;
    }

    public List<EnrollmentEntity> Enrollments { get; set; } = new();

    public List<OpenLearning.CourseManagement.Models.Course> Courses { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CourseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        Courses = await _courses.GetAllAsync();
        Enrollments = await _enrollments.GetAdminEnrollmentsAsync(CourseId, Search);
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id, string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _enrollments.RevokeAsync(id, reason ?? "admin", userId, isAdminOrFinance: true);
        if (ok)
        {
            await _logs.RecordAsync(
                userId,
                User.Identity?.Name ?? string.Empty,
                "RevokeEnrollment",
                "Enrollment",
                id.ToString(CultureInfo.InvariantCulture),
                reason ?? "admin",
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Enrollment revoked." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { CourseId, Search });
    }
}
