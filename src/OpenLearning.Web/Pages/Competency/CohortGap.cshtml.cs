using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Competency.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Competency;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class CohortGapModel : PageModel
{
    private readonly CompetencyService _competency;
    private readonly CourseService _courses;

    public CohortGapModel(CompetencyService competency, CourseService courses)
    {
        _competency = competency;
        _courses = courses;
    }

    public Course? Course { get; set; }

    public List<OpenLearning.Competency.Models.CompetencyFramework> Frameworks { get; set; } = new();

    public int? FrameworkId { get; set; }

    public List<(string UserId, int Achieved, int Partial, int Missing)> Rows { get; set; } = new();

    public Dictionary<string, string> LearnerNames { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int courseId, int? frameworkId)
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
        Frameworks = await _competency.ListFrameworksAsync(includeArchived: false);
        FrameworkId = frameworkId;

        if (frameworkId is int fid)
        {
            var (rows, error) = await _competency.GetCohortGapAsync(userId, User.IsInRole(Roles.Admin), courseId, fid);
            if (rows is null)
            {
                TempData["Message"] = error;
                TempData["MessageType"] = "danger";
            }
            else
            {
                Rows = rows;
                LearnerNames = await _competency.GetDisplayNamesAsync(rows.Select(r => r.UserId));
            }
        }

        return Page();
    }
}
