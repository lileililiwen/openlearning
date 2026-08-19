using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;

    public CreateModel(AssignmentService assignments, CourseService courses)
    {
        _assignments = assignments;
        _courses = courses;
    }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Instructions { get; set; } = string.Empty;

        [Display(Name = "Due date (optional)")]
        public DateTime? DueAt { get; set; }

        [Display(Name = "Allow resubmission after grading")]
        public bool AllowResubmitAfterGrading { get; set; }
    }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await IsOwnerAsync(courseId, userId))
        {
            return Forbid();
        }

        CourseId = courseId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await IsOwnerAsync(CourseId, userId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (ok, error) = await _assignments.CreateAsync(
            CourseId, userId, Input.Title, Input.Instructions, Input.DueAt, Input.AllowResubmitAfterGrading);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not create the assignment.");
            return Page();
        }

        return RedirectToPage("/Courses/Assignments/Index", new { courseId = CourseId });
    }

    private async Task<bool> IsOwnerAsync(int courseId, string userId)
    {
        var course = await _courses.GetByIdAsync(courseId);
        return course is not null && (course.InstructorId == userId || User.IsInRole(Roles.Admin));
    }
}
