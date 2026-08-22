using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AI.Models;
using OpenLearning.AI.Services;

namespace OpenLearning.Web.Pages.AI;

[Authorize]
public sealed class CourseModel : PageModel
{
    private readonly AiLearningService _ai;
    public CourseModel(AiLearningService ai)
    {
        _ai = ai;
    }

    public int CourseId { get; private set; }
    public List<AiMessage> Messages { get; private set; } = new();
    public List<AiFeedbackDraft> FeedbackDrafts { get; private set; } = new();
    public async Task OnGetAsync(int courseId) { CourseId = courseId; Messages = await _ai.HistoryAsync(courseId, UserId); FeedbackDrafts = await _ai.DraftsAsync(UserId); }
    public async Task<IActionResult> OnPostAskAsync(int courseId, string question, CancellationToken cancellationToken)
    {
        var result = await _ai.AskAsync(courseId, UserId, question, cancellationToken);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "AI answer generated with authorized citations." : result.Error;
        return RedirectToPage(new { courseId });
    }
    public async Task<IActionResult> OnPostFeedbackAsync(int courseId, int submissionId, CancellationToken cancellationToken)
    {
        var result = await _ai.SuggestDraftFeedbackAsync(submissionId, UserId, cancellationToken);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "AI formative feedback created; it has no grade effect." : result.Error;
        return RedirectToPage(new { courseId });
    }
    public async Task<IActionResult> OnPostReportAsync(int courseId, int messageId, string reason)
    {
        var result = await _ai.ReportAsync(messageId, UserId, reason);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "Output reported for review." : result.Error;
        return RedirectToPage(new { courseId });
    }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException();
}
