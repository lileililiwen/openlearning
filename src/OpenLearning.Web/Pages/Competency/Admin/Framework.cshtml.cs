using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Competency.Models;
using OpenLearning.Competency.Services;

namespace OpenLearning.Web.Pages.Competency.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class FrameworkModel : PageModel
{
    private readonly CompetencyService _competency;

    public FrameworkModel(CompetencyService competency)
    {
        _competency = competency;
    }

    public CompetencyFramework? Framework { get; set; }

    [BindProperty] public int? ParentId { get; set; }

    [BindProperty] public string Title { get; set; } = string.Empty;

    [BindProperty] public string? Description { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Framework = await _competency.GetFrameworkAsync(id);
        return Framework is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAddNodeAsync(int id)
    {
        var (ok, error) = await _competency.AddCompetencyAsync(id, ParentId, Title, Description ?? string.Empty);
        TempData["Message"] = ok ? "Competency added." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRenameNodeAsync(int id, int nodeId, string title, string? description)
    {
        var (ok, error) = await _competency.UpdateCompetencyAsync(nodeId, title, description ?? string.Empty);
        TempData["Message"] = ok ? "Competency updated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteNodeAsync(int id, int nodeId)
    {
        var (ok, error) = await _competency.DeleteCompetencyAsync(nodeId);
        TempData["Message"] = ok ? "Competency deleted." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
