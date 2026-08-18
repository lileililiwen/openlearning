using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly LessonService _lessons;

    public EditModel(LessonService lessons)
    {
        _lessons = lessons;
    }

    public Lesson? Lesson { get; set; }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson is null)
        {
            return NotFound();
        }

        if (lesson.Module?.Course is null || lesson.Module.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Lesson = lesson;
        Id = id;
        Input.Title = lesson.Title;
        Input.Content = lesson.Content;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            Lesson = await _lessons.GetByIdAsync(Id);
            return Page();
        }

        var updated = await _lessons.UpdateAsync(Id, userId, Input.Title, Input.Content);
        if (!updated)
        {
            return Forbid();
        }

        var courseId = (await _lessons.GetByIdAsync(Id))!.Module!.CourseId;
        return RedirectToPage("/Courses/Edit", new { id = courseId });
    }
}
