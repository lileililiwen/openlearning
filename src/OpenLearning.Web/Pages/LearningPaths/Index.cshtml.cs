using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.LearningPaths.Models;
using OpenLearning.LearningPaths.Services;

namespace OpenLearning.Web.Pages.LearningPaths;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly LearningPathService _paths;
    public IndexModel(LearningPathService paths)
    {
        _paths = paths;
    }

    public List<LearningPath> Catalog { get; private set; } = new();
    public List<PathEnrollment> Enrollments { get; private set; } = new();
    public async Task OnGetAsync() { Catalog = await _paths.CatalogAsync(); Enrollments = await _paths.ListEnrollmentsAsync(StudentId()); }
    public async Task<IActionResult> OnPostEnrollAsync(int id)
    { var result = await _paths.EnrollAsync(id, StudentId()); if (!result.Ok) { TempData["Message"] = result.Error; TempData["MessageType"] = "danger"; return RedirectToPage(); } return RedirectToPage("Roadmap", new { id = result.Enrollment!.Id }); }
    private string StudentId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
