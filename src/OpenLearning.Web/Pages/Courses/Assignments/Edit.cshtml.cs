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
public class EditModel : PageModel
{
    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;

    public EditModel(AssignmentService assignments, CourseService courses)
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
    public int Id { get; set; }

    public int CourseId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var assignment = await _assignments.GetByIdAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        if (!await IsOwnerAsync(assignment.CourseId, userId))
        {
            return Forbid();
        }

        Id = id;
        CourseId = assignment.CourseId;
        Input.Title = assignment.Title;
        Input.Instructions = assignment.Instructions;
        Input.DueAt = assignment.DueAt;
        Input.AllowResubmitAfterGrading = assignment.AllowResubmitAfterGrading;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var assignment = await _assignments.GetByIdAsync(Id);
        if (assignment is null)
        {
            return NotFound();
        }

        if (!await IsOwnerAsync(assignment.CourseId, userId))
        {
            return Forbid();
        }

        CourseId = assignment.CourseId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (ok, error) = await _assignments.UpdateAsync(
            Id, userId, Input.Title, Input.Instructions, Input.DueAt, Input.AllowResubmitAfterGrading);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not update the assignment.");
            return Page();
        }

        return RedirectToPage("/Courses/Assignments/Index", new { courseId = CourseId });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var assignment = await _assignments.GetByIdAsync(Id);
        if (assignment is null)
        {
            return NotFound();
        }

        if (!await IsOwnerAsync(assignment.CourseId, userId))
        {
            return Forbid();
        }

        var courseId = assignment.CourseId;
        await _assignments.DeleteAsync(Id, userId);
        return RedirectToPage("/Courses/Assignments/Index", new { courseId });
    }

    private async Task<bool> IsOwnerAsync(int courseId, string userId)
    {
        var course = await _courses.GetByIdAsync(courseId);
        return course is not null && (course.InstructorId == userId || User.IsInRole(Roles.Admin));
    }
}
