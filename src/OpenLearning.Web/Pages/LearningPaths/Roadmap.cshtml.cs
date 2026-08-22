using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.LearningPaths.Services;

namespace OpenLearning.Web.Pages.LearningPaths;

[Authorize]
public sealed class RoadmapModel : PageModel
{
    private readonly LearningPathService _paths;
    public RoadmapModel(LearningPathService paths)
    {
        _paths = paths;
    }

    public PathProgress Progress { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var value = await _paths.GetProgressAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (value is null)
        {
            return Forbid();
        }

        Progress = value;
        return Page();
    }
}
