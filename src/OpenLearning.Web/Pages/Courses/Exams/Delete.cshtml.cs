using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams;

[Authorize(Policy = Policies.RequireInstructor)]
public class DeleteModel : PageModel
{
    private readonly ExamService _exams;

    public DeleteModel(ExamService exams)
    {
        _exams = exams;
    }

    public Exam? Exam { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _exams.GetByIdAsync(id);
        if (exam is null)
        {
            return NotFound();
        }

        if (exam.Course is null || exam.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Exam = exam;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _exams.GetByIdAsync(id);
        if (exam is null)
        {
            return NotFound();
        }

        var deleted = await _exams.DeleteAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Details", new { id = exam.CourseId });
    }
}
