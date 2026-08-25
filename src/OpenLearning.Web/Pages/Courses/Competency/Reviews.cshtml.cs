using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Competency.Services;

namespace OpenLearning.Web.Pages.Courses.Competency;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class ReviewsModel : PageModel
{
    private readonly CompetencyService _competency;

    public ReviewsModel(CompetencyService competency)
    {
        _competency = competency;
    }

    public List<OpenLearning.Competency.Models.CompetencyEvidence> Pending { get; set; } = new();

    public Dictionary<string, string> LearnerNames { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostReviewAsync(int evidenceId, bool approve, int? levelSortOrder, string? reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _competency.ReviewEvidenceAsync(evidenceId, userId, User.IsInRole(Roles.Admin), approve, levelSortOrder, reason);
        var message = approve ? "Evidence approved." : "Evidence rejected.";
        TempData["Message"] = ok ? message : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Pending = await _competency.GetPendingReviewsAsync();
        LearnerNames = await _competency.GetDisplayNamesAsync(Pending.Select(p => p.UserId));
    }
}
