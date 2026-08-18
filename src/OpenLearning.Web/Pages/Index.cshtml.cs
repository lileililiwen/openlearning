using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public async Task OnGetAsync()
    {
        Courses = await _courses.GetPublishedCoursesAsync();
    }
}
