using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Competency.Services;

namespace OpenLearning.Web.Pages.Competency.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class IndexModel : PageModel
{
    private readonly CompetencyService _competency;

    public IndexModel(CompetencyService competency)
    {
        _competency = competency;
    }

    public List<OpenLearning.Competency.Models.CompetencyFramework> Frameworks { get; set; } = new();

    public bool ShowArchived { get; set; }

    [BindProperty] public string Name { get; set; } = string.Empty;

    [BindProperty] public string? Description { get; set; }

    [BindProperty] public string ScaleLabels { get; set; } = "Novice, Advanced Beginner, Competent, Proficient, Expert";

    public async Task OnGetAsync(bool showArchived = false)
    {
        ShowArchived = showArchived;
        Frameworks = await _competency.ListFrameworksAsync(showArchived);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var labels = (ScaleLabels ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var (ok, error) = await _competency.CreateFrameworkAsync(Name, Description ?? string.Empty, labels, "admin");
        TempData["Message"] = ok ? "Framework created." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        var (ok, error) = await _competency.SetArchivedAsync(id, true);
        TempData["Message"] = ok ? "Framework archived." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var (ok, error) = await _competency.SetArchivedAsync(id, false);
        TempData["Message"] = ok ? "Framework restored." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { showArchived = true });
    }
}
