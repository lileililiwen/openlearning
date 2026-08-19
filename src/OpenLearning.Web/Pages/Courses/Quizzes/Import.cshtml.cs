using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.QuestionIO.Models;
using OpenLearning.QuestionIO.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class ImportModel : PageModel
{
    private readonly QuizService _quizzes;
    private readonly QuestionImportService _import;

    public ImportModel(QuizService quizzes, QuestionImportService import)
    {
        _quizzes = quizzes;
        _import = import;
    }

    public Quiz? Quiz { get; set; }

    public QuestionImportOutcome? Outcome { get; set; }

    [BindProperty(SupportsGet = true)]
    public int QuizId { get; set; }

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    [BindProperty]
    public QuestionImportMode Mode { get; set; }

    public async Task<IActionResult> OnGetAsync(int quizId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsOwnerAsync(quizId, userId))
        {
            return Forbid();
        }

        Quiz = await _quizzes.GetByIdAsync(quizId);
        QuizId = quizId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int quizId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsOwnerAsync(quizId, userId))
        {
            return Forbid();
        }

        Quiz = await _quizzes.GetByIdAsync(quizId);
        QuizId = quizId;
        Outcome = await _import.ImportAsync(ExcelFile, userId, quizId, Mode, isBank: false, forceAsync: false);
        ApplyStatusCodes();
        return Page();
    }

    private void ApplyStatusCodes()
    {
        if (Outcome is null)
        {
            return;
        }

        if (Outcome.Kind == QuestionImportOutcomeKind.RateLimited)
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers["Retry-After"] = Outcome.RetryAfterSeconds?.ToString(CultureInfo.InvariantCulture) ?? "3600";
        }
        else if (Outcome.Kind == QuestionImportOutcomeKind.Error)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
