using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Admin.AsyncIO;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "异步任务")]
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
        Jobs = await _jobs.ListJobsAsync(null, isAdmin: true, Kind, Status, page);
    }
}
