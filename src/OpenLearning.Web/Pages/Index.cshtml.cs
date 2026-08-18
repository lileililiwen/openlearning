using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CourseService _courses;

    public IndexModel(CourseService courses)
    {
        _courses = courses;
    }

    public List<Course> Courses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(Roles.Admin))
            {
                return RedirectToPage("/Admin/Index");
            }

            if (User.IsInRole(Roles.Instructor))
            {
                return RedirectToPage("/Dashboard/Teacher");
            }

            if (User.IsInRole(Roles.Student))
            {
                return RedirectToPage("/Dashboard/Index");
            }
        }

        Courses = await _courses.GetPublishedCoursesAsync();
        return Page();
    }
}
