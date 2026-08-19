using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Logging.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "课程审核")]
public class CourseReviewsModel : PageModel
{
    private readonly CourseService _courses;
    private readonly LogService _logs;

    public CourseReviewsModel(CourseService courses, LogService logs)
    {
        _courses = courses;
        _logs = logs;
    }

    public List<Course> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        Courses = await _courses.GetUnderReviewAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var ok = await _courses.ApproveAsync(id, string.Empty);
        if (ok)
        {
            await _logs.RecordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.Identity?.Name ?? string.Empty,
                "ApproveCourse",
                "Course",
                id.ToString(CultureInfo.InvariantCulture),
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Course approved and published." : "Course not found.";
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string note)
    {
        var ok = await _courses.RejectAsync(id, note ?? string.Empty);
        if (ok)
        {
            await _logs.RecordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.Identity?.Name ?? string.Empty,
                "RejectCourse",
                "Course",
                id.ToString(CultureInfo.InvariantCulture),
                (note ?? string.Empty).Trim(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Course rejected and returned to draft." : "Course not found.";
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
