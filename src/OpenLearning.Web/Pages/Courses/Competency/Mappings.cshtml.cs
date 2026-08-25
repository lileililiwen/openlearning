using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.Competency.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Competency;

[Authorize(Policy = Policies.RequireInstructor)]
public class MappingsModel : PageModel
{
    private readonly CompetencyService _competency;
    private readonly CourseService _courses;
    private readonly AssignmentService _assignments;

    public MappingsModel(
        CompetencyService competency,
        CourseService courses,
        AssignmentService assignments)
    {
        _competency = competency;
        _courses = courses;
        _assignments = assignments;
    }

    public Course? Course { get; set; }

    public List<OpenLearning.Competency.Models.ActivityMapping> Mappings { get; set; } = new();

    public List<OpenLearning.Competency.Models.CompetencyFramework> Frameworks { get; set; } = new();

    public Dictionary<int, string> AssignmentTitles { get; set; } = new();

    [BindProperty] public int CompetencyId { get; set; }

    [BindProperty] public int? AssignmentId { get; set; }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var load = await LoadAsync(courseId);
        return load ?? Page();
    }

    public async Task<IActionResult> OnPostMapAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (ok, error) = AssignmentId is int assignmentId
            ? await _competency.MapAssignmentAsync(assignmentId, CompetencyId, userId, isAdmin)
            : await _competency.MapCourseAsync(courseId, CompetencyId, userId, isAdmin);
        TempData["Message"] = ok ? "Mapping added." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostUnmapAsync(int courseId, int mappingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _competency.UnmapAsync(mappingId, userId, User.IsInRole(Roles.Admin));
        TempData["Message"] = ok ? "Mapping removed." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    private async Task<IActionResult?> LoadAsync(int courseId)
    {
        var course = await _courses.GetByIdAsync(courseId);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (course.InstructorId != userId && !User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        Course = course;
        Mappings = await _competency.GetCourseMappingsAsync(courseId);
        Frameworks = await _competency.ListFrameworksAsync(includeArchived: false);

        var assignments = await _assignments.GetForCourseAsync(courseId);
        AssignmentTitles = assignments.ToDictionary(a => a.Id, a => a.Title);

        return null;
    }
}
