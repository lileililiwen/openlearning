using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Logging.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "教师工作台:/Dashboard/Teacher", "课程管理")]
public class ManageModel : PageModel
{
    private readonly CourseService _courses;
    private readonly LogService _logs;

    public ManageModel(CourseService courses, LogService logs)
    {
        _courses = courses;
        _logs = logs;
    }

    public List<Course> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Courses = await _courses.GetByInstructorAsync(userId);
    }

    public async Task<IActionResult> OnPostPublishAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var newStatus = course.IsPublished ? CourseStatus.Draft : CourseStatus.Published;
        var (ok, error) = await _courses.SetStatusAsync(id, userId, newStatus);
        if (ok)
        {
            await _logs.RecordAsync(
                userId,
                User.Identity?.Name ?? string.Empty,
                course.IsPublished ? "UnpublishCourse" : "PublishCourse",
                "Course",
                id.ToString(CultureInfo.InvariantCulture),
                course.Title,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
        else
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var deleted = await _courses.DeleteAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        await _logs.RecordAsync(
            userId,
            User.Identity?.Name ?? string.Empty,
            "DeleteCourse",
            "Course",
            id.ToString(CultureInfo.InvariantCulture),
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToPage();
    }
}
