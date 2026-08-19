using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages.Courses.Live;

public class EditModel : PageModel
{
    private readonly CourseService _courses;
    private readonly LiveService _live;

    public EditModel(CourseService courses, LiveService live)
    {
        _courses = courses;
        _live = live;
    }

    public Course? Course { get; set; }

    public LiveSession? Session { get; set; }

    [BindProperty]
    public EditInputModel Input { get; set; } = new();

    public class EditInputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartsAt { get; set; }

        [Required]
        public DateTime EndsAt { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var session = await _live.GetByIdAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (session.InstructorId != userId)
        {
            return Forbid();
        }

        var course = await _courses.GetByIdAsync(session.CourseId);
        if (course is null)
        {
            return NotFound();
        }

        Course = course;
        Session = session;
        Input = new EditInputModel
        {
            Title = session.Title,
            Description = session.Description,
            StartsAt = session.StartsAt.ToLocalTime(),
            EndsAt = session.EndsAt.ToLocalTime(),
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidSession = await _live.GetByIdAsync(id);
            return RedirectToPage("Index", new { id = invalidSession?.CourseId ?? 0 });
        }

        var input = new LiveInput(
            Input.Title,
            Input.Description,
            DateTime.SpecifyKind(Input.StartsAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(Input.EndsAt, DateTimeKind.Utc));
        var (ok, error) = await _live.UpdateAsync(id, userId, input);
        var session = await _live.GetByIdAsync(id);
        var courseId = session?.CourseId ?? 0;
        TempData["Message"] = ok ? "Live session updated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage("Index", new { id = courseId });
    }
}
