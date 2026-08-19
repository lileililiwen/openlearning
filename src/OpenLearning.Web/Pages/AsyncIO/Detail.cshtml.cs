using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.AsyncIO;

public class DetailModel : PageModel
{
    private readonly AsyncIOService _jobs;

    public DetailModel(AsyncIOService jobs)
    {
        _jobs = jobs;
    }

    public AsyncIOJob? Job { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        Job = await _jobs.GetJobAsync(id, userId, isAdmin);
        return Job is null ? Forbid() : Page();
    }
}
