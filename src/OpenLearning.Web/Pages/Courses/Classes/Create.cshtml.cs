using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Classes.Services;

namespace OpenLearning.Web.Pages.Courses.Classes;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly ClassGroupService _classes;

    public CreateModel(ClassGroupService classes)
    {
        _classes = classes;
    }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public DateTime StartsAt { get; set; } = DateTime.UtcNow.AddDays(7);

        public DateTime EndsAt { get; set; } = DateTime.UtcNow.AddDays(7 * 30);

        public int? Capacity { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        CourseId = courseId;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _classes.IsCourseOwnerAsync(courseId, userId))
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

        var (classGroup, error) = await _classes.CreateAsync(
            CourseId, userId, Input.Name, ToUtc(Input.StartsAt), ToUtc(Input.EndsAt), Input.Capacity);
        if (classGroup is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create the class.");
            return Page();
        }

        return RedirectToPage("/Courses/Classes/Manage", new { id = classGroup.Id });
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    }
}
