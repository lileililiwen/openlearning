using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

public class ResultModel : PageModel
{
    private readonly AttemptService _attempts;

    public ResultModel(AttemptService attempts)
    {
        _attempts = attempts;
    }

    public QuizAttempt? Attempt { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var attempt = await _attempts.GetAttemptAsync(id, userId);
        if (attempt is null)
        {
            return Forbid();
        }

        Attempt = attempt;
        return Page();
    }
}
