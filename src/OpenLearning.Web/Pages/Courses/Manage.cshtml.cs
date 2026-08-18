using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
public class ManageModel : PageModel
{
    private readonly CourseService _courses;

    public ManageModel(CourseService courses)
    {
        _courses = courses;
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
        await _courses.SetStatusAsync(id, userId, newStatus);
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

        return RedirectToPage();
    }
}
