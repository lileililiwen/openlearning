using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.PracticalTraining.Models;
using OpenLearning.PracticalTraining.Services;

namespace OpenLearning.Web.Pages.PracticalTraining;

[AllowAnonymous]
public sealed class SupervisorModel : PageModel
{
    private readonly PracticalTrainingService _service;
    public SupervisorModel(PracticalTrainingService service)
    {
        _service = service;
    }

    public Placement? Placement { get; private set; }
    public List<PracticalHourLog> Logs { get; private set; } = new();
    public List<PlacementCompetency> Competencies { get; private set; } = new();
    public async Task<IActionResult> OnGetAsync(string token)
    {
        Placement = await _service.ResolveSupervisorAsync(token);
        if (Placement is null)
        {
            return NotFound();
        }
        Logs = await _service.ListLogsAsync(Placement.Id);
        Competencies = await _service.ListCompetenciesAsync(Placement.Id);
        return Page();
    }
    public async Task<IActionResult> OnPostReviewAsync(string token, int placementId, int logId, Guid concurrencyStamp, bool approve, string? note)
    {
        var result = await _service.ReviewHoursAsync(token, placementId, logId, concurrencyStamp, approve, note);
        Flash(result.Error ?? "Log reviewed.", result.Ok);
        return RedirectToPage(new { token });
    }
    public async Task<IActionResult> OnPostCompetencyAsync(string token, int placementId, int competencyId, bool achieved, string evaluation)
    {
        var result = await _service.EvaluateCompetencyAsync(token, placementId, competencyId, achieved, evaluation);
        Flash(result.Error ?? "Competency evaluated.", result.Ok);
        return RedirectToPage(new { token });
    }
    public async Task<IActionResult> OnPostEvaluationAsync(string token, int placementId, string summary)
    {
        var result = await _service.SubmitEvaluationAsync(token, placementId, summary);
        Flash(result.Error ?? "Evaluation submitted.", result.Ok);
        return RedirectToPage(new { token });
    }
    private void Flash(string message, bool ok)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
