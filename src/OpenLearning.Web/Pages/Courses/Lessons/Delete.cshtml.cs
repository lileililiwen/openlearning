using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

[Authorize(Policy = Policies.RequireInstructor)]
public class DeleteModel : PageModel
{
    private readonly LessonService _lessons;

    public DeleteModel(LessonService lessons)
    {
        _lessons = lessons;
    }

    public Lesson? Lesson { get; set; }

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
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson is null)
        {
            return NotFound();
        }

        var deleted = await _lessons.DeleteAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Edit", new { id = lesson.Module!.CourseId });
    }
}
