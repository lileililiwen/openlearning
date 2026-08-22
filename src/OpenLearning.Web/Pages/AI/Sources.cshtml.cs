using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AI.Models;
using OpenLearning.AI.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.AI;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public sealed class SourcesModel : PageModel
{
    private readonly AiLearningService _ai;
    public SourcesModel(AiLearningService ai)
    {
        _ai = ai;
    }

    public int CourseId { get; private set; }
    public List<AiApprovedSource> Sources { get; private set; } = new();
    public async Task OnGetAsync(int courseId) { CourseId = courseId; Sources = await _ai.SourcesAsync(courseId); }
    public async Task<IActionResult> OnPostAddAsync(int courseId, string title, string anchor, string content, bool published, bool approved)
    {
        try
        { var source = await _ai.AddSourceAsync(courseId, UserId, User.IsInRole("Admin"), title, anchor, content, published, approved); TempData[source.IsUnsafe ? "Error" : "Success"] = source.IsUnsafe ? "Prompt-injection content was quarantined." : "Source indexed."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return RedirectToPage(new { courseId });
    }
    public async Task<IActionResult> OnPostRemoveAsync(int courseId, int sourceId)
    {
        if (!await _ai.RemoveSourceAsync(sourceId, UserId, User.IsInRole("Admin")))
        {
            return Forbid();
        }
        return RedirectToPage(new { courseId });
    }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException();
}
