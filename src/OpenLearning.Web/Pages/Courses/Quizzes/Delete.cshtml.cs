using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class DeleteModel : PageModel
{
    private readonly QuizService _quizzes;

    public DeleteModel(QuizService quizzes)
    {
        _quizzes = quizzes;
    }

    public Quiz? Quiz { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var quiz = await _quizzes.GetByIdAsync(id);
        if (quiz is null)
        {
            return NotFound();
        }

        if (quiz.Course is null || quiz.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Quiz = quiz;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var quiz = await _quizzes.GetByIdAsync(id);
        if (quiz is null)
        {
            return NotFound();
        }

        var deleted = await _quizzes.DeleteAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Edit", new { id = quiz.CourseId });
    }
}
