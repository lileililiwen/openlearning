using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Modules;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly ModuleService _modules;

    public CreateModel(ModuleService modules)
    {
        _modules = modules;
    }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        CourseId = courseId;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _modules.IsOwnerAsync(courseId, userId))
        {
            return Forbid();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var module = await _modules.AddAsync(CourseId, userId, Input.Title);
        if (module is null)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Edit", new { id = CourseId });
    }
}
