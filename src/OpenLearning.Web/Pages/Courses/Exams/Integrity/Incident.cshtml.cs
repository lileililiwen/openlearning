using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams.Integrity;

/// <summary>Reviewer incident detail: evidence, explainable score, disposition.</summary>
public class IncidentModel : PageModel
{
    private readonly ExamIntegrityService _integrity;

    public IncidentModel(ExamIntegrityService integrity)
    {
        _integrity = integrity;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public IntegrityIncident? Incident { get; set; }

    public List<IntegrityEvidence> Evidence { get; set; } = new();

    public List<RiskContributionView> Contributions { get; set; } = new();

    [BindProperty]
    public IntegrityDispositionOutcome Outcome { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public class RiskContributionView
    {
        public string Rule { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int Count { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        Incident = await _integrity.GetIncidentForReviewAsync(Id, userId);
        if (Incident is null)
        {
            return Forbid();
        }

        Evidence = await _integrity.GetEvidenceForReviewAsync(Id, userId);
        try
        {
            Contributions = JsonSerializer.Deserialize<List<RiskContributionView>>(Incident.ContributingRules) ?? new();
        }
        catch (JsonException)
        {
            Contributions = new();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _integrity.RecordDispositionAsync(Id, userId, Outcome, Notes);
        if (result.Error is not null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await OnGetAsync();
            return Page();
        }

        return RedirectToPage("/Courses/Exams/Integrity/Incidents");
    }
}
