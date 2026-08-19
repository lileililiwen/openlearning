using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams;

public class ResultModel : PageModel
{
    private readonly ExamService _exams;

    public ResultModel(ExamService exams)
    {
        _exams = exams;
    }

    public ExamAttempt? Attempt { get; set; }

    /// <summary>True when the viewer is the course instructor (can grade manual answers).</summary>
    public bool IsInstructor { get; set; }

    /// <summary>This attempt's position within the student's attempts (1-based).</summary>
    public int AttemptNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var attempt = await _exams.GetAttemptAsync(id, userId);
        if (attempt is null)
        {
            return Forbid();
        }

        IsInstructor = attempt.Exam?.Course is not null && attempt.Exam.Course.InstructorId == userId;
        if (!IsInstructor)
        {
            IsInstructor = User.IsInRole(Roles.Admin);
        }

        var prior = await _exams.GetPriorCompletedCountAsync(attempt.ExamId, attempt.StudentId, attempt.Id);
        AttemptNumber = prior + 1;
        Attempt = attempt;
        return Page();
    }

    public async Task<IActionResult> OnPostGradeAsync(int answerId, int score, string? feedback)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _exams.GradeAsync(answerId, score, feedback, userId);
        TempData["Message"] = ok ? "Answer graded." : error;
        TempData["MessageType"] = ok ? "success" : "danger";

        var answer = await _exams.GetAnswerForAttemptAsync(answerId, userId);
        return answer is null
            ? Forbid()
            : RedirectToPage(new { id = answer.AttemptId });
    }
}
