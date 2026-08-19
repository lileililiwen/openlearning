using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.QuestionIO.Models;
using OpenLearning.QuestionIO.Services;

namespace OpenLearning.Web.Pages.Admin.QuestionBank;

[Authorize(Policy = Policies.RequireAdmin)]
public class ImportModel : PageModel
{
    private readonly QuestionImportService _import;

    public ImportModel(QuestionImportService import)
    {
        _import = import;
    }

    public QuestionImportOutcome? Outcome { get; set; }

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    [BindProperty]
    public QuestionImportMode Mode { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Outcome = await _import.ImportAsync(ExcelFile, userId, quizId: null, Mode, isBank: true, forceAsync: false);
        if (Outcome.Kind == QuestionImportOutcomeKind.RateLimited)
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers["Retry-After"] = Outcome.RetryAfterSeconds?.ToString(CultureInfo.InvariantCulture) ?? "3600";
        }
        else if (Outcome.Kind == QuestionImportOutcomeKind.Error)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        return Page();
    }
}
