using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Competency.Services;

namespace OpenLearning.Web.Pages.Competency;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly CompetencyService _competency;

    public ProfileModel(CompetencyService competency)
    {
        _competency = competency;
    }

    public string LearnerId { get; set; } = string.Empty;

    public string LearnerName { get; set; } = string.Empty;

    public bool IsSelf { get; set; }

    public List<CompetencyService.ProfileRow> Rows { get; set; } = new();

    public int? FrameworkFilter { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, int? frameworkId)
    {
        var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var learnerId = string.IsNullOrWhiteSpace(userId) ? viewerId : userId;
        if (learnerId != viewerId &&
            !User.IsInRole(Roles.Admin) &&
            !await _competency.CanViewLearnerAsync(viewerId, learnerId))
        {
            return Forbid();
        }

        LearnerId = learnerId;
        IsSelf = learnerId == viewerId;
        FrameworkFilter = frameworkId;
        Rows = await _competency.GetProfileAsync(learnerId);

        if (frameworkId is int fid)
        {
            Rows = Rows.Where(r => r.Framework.Id == fid).ToList();
        }

        var names = await _competency.GetDisplayNamesAsync(new[] { learnerId });
        LearnerName = names.GetValueOrDefault(learnerId, learnerId);

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitEvidenceAsync(int competencyId, string description, string? attachmentUrl)
    {
        var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _competency.SubmitManualEvidenceAsync(viewerId, competencyId, description, attachmentUrl);
        TempData["Message"] = ok ? "Evidence submitted for review." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
