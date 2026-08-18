using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes.Questions;

[Authorize(Policy = Policies.RequireInstructor)]
public class DeleteModel : PageModel
{
    private readonly QuestionService _questions;

    public DeleteModel(QuestionService questions)
    {
        _questions = questions;
    }

    public Question? Question { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var question = await _questions.GetByIdAsync(id);
        if (question is null)
        {
            return NotFound();
        }

        if (question.Quiz?.Course is null || question.Quiz.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Question = question;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var question = await _questions.GetByIdAsync(id);
        if (question is null)
        {
            return NotFound();
        }

        var deleted = await _questions.DeleteAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Quizzes/Edit", new { id = question.QuizId });
    }
}
