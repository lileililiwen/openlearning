using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.AsyncIO;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AsyncIOService _jobs;

    public IndexModel(AsyncIOService jobs)
    {
        _jobs = jobs;
    }

    public List<AsyncIOJob> Jobs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Kind { get; set; }

    [BindProperty(SupportsGet = true)]
    public AsyncIOJobStatus? Status { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        Jobs = await _jobs.ListJobsAsync(userId, isAdmin, Kind, Status, page);
    }
}
