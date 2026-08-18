using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Modules;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly ModuleService _modules;

    public EditModel(ModuleService modules)
    {
        _modules = modules;
    }

    public Module? Module { get; set; }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
    }

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
        Id = id;
        Input.Title = module.Title;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            var module = await _modules.GetByIdAsync(Id);
            if (module is not null)
            {
                Module = module;
            }

            return Page();
        }

        var updated = await _modules.UpdateAsync(Id, userId, Input.Title);
        if (!updated)
        {
            return Forbid();
        }

        var courseId = (await _modules.GetByIdAsync(Id))!.CourseId;
        return RedirectToPage("/Courses/Edit", new { id = courseId });
    }
}
