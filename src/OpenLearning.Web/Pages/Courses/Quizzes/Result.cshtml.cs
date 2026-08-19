using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

public class ResultModel : PageModel
{
    private readonly AttemptService _attempts;

    public ResultModel(AttemptService attempts)
    {
        _attempts = attempts;
    }

    public QuizAttempt? Attempt { get; set; }

    /// <summary>True when the viewer is the course instructor (can grade manual answers).</summary>
    public bool IsInstructor { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var attempt = await _attempts.GetAttemptAsync(id, userId);
        if (attempt is null)
        {
            return Forbid();
        }

        IsInstructor = attempt.Quiz?.Course is not null && attempt.Quiz.Course.InstructorId == userId;
        if (!IsInstructor)
        {
            IsInstructor = User.IsInRole(Roles.Admin);
        }

        Attempt = attempt;
        return Page();
    }

    public async Task<IActionResult> OnPostGradeAsync(int answerId, int score, string? feedback)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _attempts.GradeAsync(answerId, score, feedback, userId);
        TempData["Message"] = ok ? "Answer graded." : error;
        TempData["MessageType"] = ok ? "success" : "danger";

        var answer = await _attempts.GetAnswerForAttemptAsync(answerId, userId);
        return answer is null
            ? Forbid()
            : RedirectToPage(new { id = answer.AttemptId });
    }
}
