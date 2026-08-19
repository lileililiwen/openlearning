using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;

namespace OpenLearning.Web.Pages.Courses.Classes;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly ClassGroupService _classes;

    public EditModel(ClassGroupService classes)
    {
        _classes = classes;
    }

    public ClassGroup? ClassGroup { get; set; }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime StartsAt { get; set; }

        public DateTime EndsAt { get; set; }

        public int? Capacity { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var classGroup = await _classes.GetByIdAsync(id);
        if (classGroup is null)
        {
            return NotFound();
        }

        if (classGroup.Course is null || classGroup.Course.InstructorId != userId)
        {
            return Forbid();
        }

        ClassGroup = classGroup;
        Id = id;
        Input.Name = classGroup.Name;
        Input.StartsAt = classGroup.StartsAt;
        Input.EndsAt = classGroup.EndsAt;
        Input.Capacity = classGroup.Capacity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!ModelState.IsValid)
        {
            ClassGroup = await _classes.GetByIdAsync(Id);
            return Page();
        }

        var (ok, error) = await _classes.UpdateAsync(
            Id, userId, Input.Name, ToUtc(Input.StartsAt), ToUtc(Input.EndsAt), Input.Capacity);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to save the class.");
            ClassGroup = await _classes.GetByIdAsync(Id);
            return Page();
        }

        return RedirectToPage("/Courses/Classes/Manage", new { id = Id });
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    }
}
