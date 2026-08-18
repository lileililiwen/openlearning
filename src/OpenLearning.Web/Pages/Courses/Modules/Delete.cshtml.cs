using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Modules;

[Authorize(Policy = Policies.RequireInstructor)]
public class DeleteModel : PageModel
{
    private readonly ModuleService _modules;

    public DeleteModel(ModuleService modules)
    {
        _modules = modules;
    }

    public Module? Module { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var module = await _modules.GetByIdAsync(id);
        if (module is null)
        {
            return NotFound();
        }

        if (module.Course is null || module.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Module = module;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var module = await _modules.GetByIdAsync(id);
        if (module is null)
        {
            return NotFound();
        }

        var deleted = await _modules.DeleteAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Edit", new { id = module.CourseId });
    }
}
