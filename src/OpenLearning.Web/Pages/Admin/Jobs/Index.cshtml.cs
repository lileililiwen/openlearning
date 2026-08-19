using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Jobs.Services;

namespace OpenLearning.Web.Pages.Admin.Jobs;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "任务调度")]
public class IndexModel : PageModel
{
    private readonly JobAdminService _jobs;

    public IndexModel(JobAdminService jobs)
    {
        _jobs = jobs;
    }

    public List<JobSummary> Jobs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Jobs = await _jobs.GetAllAsync();
    }

    public static string RateText(double rate)
    {
        return rate.ToString("0%", CultureInfo.InvariantCulture);
    }
}
