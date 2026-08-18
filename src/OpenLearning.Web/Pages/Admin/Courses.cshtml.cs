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
public class CoursesModel : PageModel
{
    private readonly CourseService _courses;
    private readonly LogService _logs;

    public CoursesModel(CourseService courses, LogService logs)
    {
        _courses = courses;
        _logs = logs;
    }

    public List<Course> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        Courses = await _courses.GetAllAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _courses.DeleteAnyAsync(id);
        await _logs.RecordAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name ?? string.Empty,
            "DeleteCourse",
            "Course",
            id.ToString(CultureInfo.InvariantCulture),
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToPage();
    }
}
