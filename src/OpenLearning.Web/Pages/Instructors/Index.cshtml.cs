using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Instructors;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CourseService _courses;

    public IndexModel(UserManager<ApplicationUser> userManager, CourseService courses)
    {
        _userManager = userManager;
        _courses = courses;
    }

    public ApplicationUser? Instructor { get; set; }

    public List<Course> PublishedCourses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var instructor = await _userManager.FindByIdAsync(id);
        if (instructor is null)
        {
            return NotFound();
        }

        Instructor = instructor;
        var courses = await _courses.GetByInstructorAsync(id);
        PublishedCourses = courses
            .Where(c => c.IsPublished)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Page();
    }
}
