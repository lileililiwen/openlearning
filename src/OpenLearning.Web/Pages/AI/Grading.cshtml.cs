using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AI.Models;
using OpenLearning.AI.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.AI;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public sealed class GradingModel : PageModel
{
    private readonly AiLearningService _ai;
    public GradingModel(AiLearningService ai)
    {
        _ai = ai;
    }

    public List<AiFeedbackDraft> Drafts { get; private set; } = new();
    public async Task OnGetAsync()
    {
        Drafts = await _ai.DraftsAsync(UserId);
    }

    public async Task<IActionResult> OnPostSuggestAsync(int submissionId, CancellationToken cancellationToken) { var result = await _ai.SuggestGradeAsync(submissionId, UserId, cancellationToken); TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "Advisory draft created." : result.Error; return RedirectToPage(); }
    public async Task<IActionResult> OnPostConfirmAsync(int draftId, int score, string feedback) { var result = await _ai.ConfirmGradeAsync(draftId, UserId, score, feedback); TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "Final grade confirmed by human grader." : result.Error; return RedirectToPage(); }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException();
}
