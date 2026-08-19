using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;

namespace OpenLearning.Web.Pages.Courses.Classes;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class IndexModel : PageModel
{
    private readonly ClassGroupService _classes;

    public IndexModel(ClassGroupService classes)
    {
        _classes = classes;
    }

    public List<ClassGroup> Classes { get; set; } = new();

    public int CourseId { get; set; }

    public bool IsOwner { get; set; }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsOwner = User.IsInRole(Roles.Admin) || await _classes.IsCourseOwnerAsync(courseId, userId);
        if (!IsOwner)
        {
            return Forbid();
        }

        CourseId = courseId;
        Classes = await _classes.GetForCourseAsync(courseId);
        return Page();
    }
}
