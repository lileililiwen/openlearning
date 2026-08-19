using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Jobs.Services;

namespace OpenLearning.Web.Pages.Admin.Jobs;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "任务调度", "任务详情")]
public class DetailModel : PageModel
{
    private readonly JobAdminService _jobs;

    public DetailModel(JobAdminService jobs)
    {
        _jobs = jobs;
    }

    public JobDetail? Detail { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Detail = await _jobs.GetDetailAsync(id);
        return Detail is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostRunAsync(int id)
    {
        await _jobs.RunNowAsync(id);
        TempData["Message"] = "Job run triggered.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPauseAsync(int id)
    {
        await _jobs.SetEnabledAsync(id, enabled: false);
        TempData["Message"] = "Job paused.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResumeAsync(int id)
    {
        await _jobs.SetEnabledAsync(id, enabled: true);
        TempData["Message"] = "Job resumed.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCronAsync(int id, string cron)
    {
        var (ok, error) = await _jobs.UpdateCronAsync(id, cron ?? string.Empty);
        TempData["Message"] = ok ? "Cron updated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
