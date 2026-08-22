using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.PracticalTraining.Models;
using OpenLearning.PracticalTraining.Services;

namespace OpenLearning.Web.Pages.PracticalTraining;

[Authorize(Policy = Policies.RequireAdmin)]
public sealed class ManageModel : PageModel
{
    private readonly PracticalTrainingService _service;
    public ManageModel(PracticalTrainingService service)
    {
        _service = service;
    }

    public List<PracticalProgram> Programs { get; private set; } = new();
    public List<HostOrganization> Hosts { get; private set; } = new();
    public List<Placement> Placements { get; private set; } = new();
    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostProgramAsync(string title, decimal minimumHours, string competencies)
    {
        try
        {
            await _service.CreateProgramAsync(title, minimumHours, competencies.Split(',', StringSplitOptions.RemoveEmptyEntries));
            Flash("Program created.", true);
        }
        catch (ArgumentException ex)
        {
            Flash(ex.Message, false);
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostHostAsync(string name, string? email)
    {
        try
        {
            await _service.CreateHostAsync(name, email);
            Flash("Host created.", true);
        }
        catch (ArgumentException ex)
        {
            Flash(ex.Message, false);
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostPlacementAsync(int programId, int hostId, string learnerId, string supervisorName,
        string supervisorEmail, DateOnly? startsOn, DateOnly? endsOn)
    {
        try
        {
            await _service.CreatePlacementAsync(programId, hostId, learnerId, ActorId, supervisorName, supervisorEmail, startsOn, endsOn);
            Flash("Placement created.", true);
        }
        catch (ArgumentException ex)
        {
            Flash(ex.Message, false);
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostActivateAsync(int id)
    {
        var result = await _service.ActivateAsync(id, ActorId, true);
        Flash(result.Error ?? "Placement activated.", result.Ok);
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostInviteAsync(int id)
    {
        var result = await _service.InviteSupervisorAsync(id, ActorId, true, TimeSpan.FromDays(7));
        Flash(result.Error ?? $"Supervisor link: {Url.Page("Supervisor", null, new { token = result.Token }, Request.Scheme)}", result.Token is not null);
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostRevokeAsync(int id)
    {
        var result = await _service.RevokeSupervisorAsync(id, ActorId, true);
        Flash(result.Error ?? "Supervisor access revoked.", result.Ok);
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostIncidentAsync(int id, IncidentSeverity severity, string summary)
    {
        try
        {
            await _service.ReportIncidentAsync(id, ActorId, true, severity, summary);
            Flash("Incident recorded.", true);
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException)
        {
            Flash("Incident could not be recorded.", false);
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostResolveIncidentAsync(int id, int incidentId, string resolution)
    {
        var result = await _service.ResolveIncidentAsync(id, incidentId, ActorId, true, resolution);
        Flash(result.Error ?? "Incident resolved.", result.Ok);
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var result = await _service.ConfirmCompletionAsync(id, ActorId, true);
        Flash(result.Error ?? $"Completion confirmed ({result.Completion!.ConfirmationKey}).", result.Ok);
        return RedirectToPage();
    }
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task LoadAsync()
    {
        Programs = await _service.ListProgramsAsync();
        Hosts = await _service.ListHostsAsync();
        Placements = await _service.ListForCoordinatorAsync(ActorId, true);
    }
    private void Flash(string message, bool ok)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
