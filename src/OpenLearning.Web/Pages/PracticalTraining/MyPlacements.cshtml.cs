using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.PracticalTraining.Models;
using OpenLearning.PracticalTraining.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.Web.Pages.PracticalTraining;

[Authorize(Policy = Policies.RequireStudent)]
public sealed class MyPlacementsModel : PageModel
{
    private readonly PracticalTrainingService _service;
    private readonly StorageService _storage;
    public MyPlacementsModel(PracticalTrainingService service, StorageService storage)
    {
        _service = service;
        _storage = storage;
    }

    public List<Placement> Placements { get; private set; } = new();
    public List<PracticalHourLog> Logs { get; private set; } = new();
    public List<PlacementCompetency> Competencies { get; private set; } = new();
    public int? SelectedId { get; private set; }
    public async Task OnGetAsync(int? id)
    {
        Placements = await _service.ListForLearnerAsync(ActorId);
        SelectedId = id;
        if (id is not null && Placements.Any(x => x.Id == id))
        {
            Logs = await _service.ListLogsAsync(id.Value);
            Competencies = await _service.ListCompetenciesAsync(id.Value);
        }
    }
    public async Task<IActionResult> OnPostHoursAsync(int id, DateTime startedAt, DateTime endedAt, string? description, int? amendsLogId)
    {
        var result = await _service.SubmitHoursAsync(id, ActorId, startedAt, endedAt, description, amendsLogId);
        Flash(result.Error ?? "Hours submitted.", result.Ok);
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostEvidenceAsync(int id, IFormFile evidence, string? description)
    {
        if (evidence is null)
        {
            Flash("Evidence file is required.", false);
            return RedirectToPage(new { id });
        }
        await using var stream = evidence.OpenReadStream();
        var upload = await _storage.UploadAsync(ActorId, FilePurpose.Answer, evidence.FileName, evidence.ContentType, stream);
        if (upload.File is null)
        {
            Flash(upload.Error!, false);
            return RedirectToPage(new { id });
        }
        var result = await _service.AddEvidenceAsync(id, ActorId, upload.File.Id, description);
        Flash(result.Error ?? "Evidence submitted.", result.Ok);
        return RedirectToPage(new { id });
    }
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static string EvaluationStatus(PlacementCompetency item)
    {
        if (item.EvaluatedAt is null)
        {
            return "Not evaluated";
        }
        return item.IsAchieved ? "Achieved" : "Not achieved";
    }
    private void Flash(string message, bool ok)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
