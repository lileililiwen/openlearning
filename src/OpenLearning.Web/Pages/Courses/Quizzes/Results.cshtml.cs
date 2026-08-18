using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class ResultsModel : PageModel
{
    private readonly QuizService _quizzes;
    private readonly AttemptService _attempts;

    public ResultsModel(QuizService quizzes, AttemptService attempts)
    {
        _quizzes = quizzes;
        _attempts = attempts;
    }

    public Quiz? Quiz { get; set; }

    public List<QuizAttempt> Attempts { get; set; } = new();

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
        Attempts = await _attempts.GetAttemptsForQuizAsync(id, userId);
        return Page();
    }
}
