using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Logging.Services;
using OpenLearning.Operations.Models;
using OpenLearning.Operations.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class OperationsModel : PageModel
{
    private readonly OperationsService _operations;
    private readonly CourseService _courses;
    private readonly LogService _logs;

    public OperationsModel(OperationsService operations, CourseService courses, LogService logs)
    {
        _operations = operations;
        _courses = courses;
        _logs = logs;
    }

    public string Tab { get; set; } = "banners";

    public List<Banner> Banners { get; set; } = new();

    public List<Popup> Popups { get; set; } = new();

    public List<Campaign> Campaigns { get; set; } = new();

    public List<HomepageFeature> Features { get; set; } = new();

    public List<Course> AllCourses { get; set; } = new();

    public List<int> FeatureCourseIds { get; set; } = new();

    public async Task OnGetAsync(string tab = "banners")
    {
        Tab = tab;
        Banners = await _operations.GetAllBannersAsync();
        Popups = await _operations.GetAllPopupsAsync();
        Campaigns = await _operations.GetAllCampaignsAsync();
        Features = await _operations.GetHomepageFeaturesAsync();
        AllCourses = await _courses.GetAllAsync();
        FeatureCourseIds = Features.Where(f => f.CourseId is not null).Select(f => f.CourseId!.Value).ToList();
    }

    public async Task<IActionResult> OnPostCreateBannerAsync(string title, string imageUrl, string linkUrl, int? campaignId)
    {
        var (ok, error) = await _operations.CreateBannerAsync(title, imageUrl, linkUrl, campaignId);
        await LogAsync(ok, "CreateBanner", title, error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "banners" });
    }

    public async Task<IActionResult> OnPostToggleBannerAsync(int id)
    {
        var banner = await _operations.GetBannerByIdAsync(id);
        if (banner is null)
        {
            return NotFound();
        }

        var (ok, error) = await _operations.UpdateBannerAsync(id, banner.Title, banner.ImageUrl, banner.LinkUrl, banner.CampaignId, !banner.IsActive);
        await LogAsync(ok, "ToggleBanner", banner.Title, error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "banners" });
    }

    public async Task<IActionResult> OnPostMoveBannerAsync(int id, int direction)
    {
        var banner = await _operations.GetBannerByIdAsync(id);
        if (banner is null)
        {
            return NotFound();
        }

        var siblings = (await _operations.GetAllBannersAsync()).OrderBy(b => b.OrderIndex).ToList();
        var index = siblings.FindIndex(b => b.Id == id);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= siblings.Count)
        {
            Flash(false, "Cannot move the banner further.");
            return RedirectToPage(new { tab = "banners" });
        }

        var swapWith = siblings[target];
        await _operations.SetBannerOrderAsync(id, swapWith.OrderIndex);
        await _operations.SetBannerOrderAsync(swapWith.Id, banner.OrderIndex);
        await LogAsync(true, "MoveBanner", banner.Title, null);
        Flash(true, null);
        return RedirectToPage(new { tab = "banners" });
    }

    public async Task<IActionResult> OnPostDeleteBannerAsync(int id)
    {
        var (ok, error) = await _operations.DeleteBannerAsync(id);
        await LogAsync(ok, "DeleteBanner", id.ToString(CultureInfo.InvariantCulture), error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "banners" });
    }

    public async Task<IActionResult> OnPostCreatePopupAsync(string title, string body, string linkUrl, DateTime startsAt, DateTime endsAt)
    {
        var (ok, error) = await _operations.CreatePopupAsync(
            title, body, linkUrl, Normalize(startsAt), Normalize(endsAt));
        await LogAsync(ok, "CreatePopup", title, error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "popups" });
    }

    public async Task<IActionResult> OnPostTogglePopupAsync(int id)
    {
        var (ok, error) = await _operations.TogglePopupAsync(id);
        await LogAsync(ok, "TogglePopup", id.ToString(CultureInfo.InvariantCulture), error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "popups" });
    }

    public async Task<IActionResult> OnPostDeletePopupAsync(int id)
    {
        var (ok, error) = await _operations.DeletePopupAsync(id);
        await LogAsync(ok, "DeletePopup", id.ToString(CultureInfo.InvariantCulture), error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "popups" });
    }

    public async Task<IActionResult> OnPostCreateCampaignAsync(string name, DateTime startsAt, DateTime endsAt)
    {
        var (ok, error) = await _operations.CreateCampaignAsync(name, Normalize(startsAt), Normalize(endsAt));
        await LogAsync(ok, "CreateCampaign", name, error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "campaigns" });
    }

    public async Task<IActionResult> OnPostToggleCampaignAsync(int id)
    {
        var (ok, error) = await _operations.ToggleCampaignAsync(id);
        await LogAsync(ok, "ToggleCampaign", id.ToString(CultureInfo.InvariantCulture), error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "campaigns" });
    }

    public async Task<IActionResult> OnPostDeleteCampaignAsync(int id)
    {
        var (ok, error) = await _operations.DeleteCampaignAsync(id);
        await LogAsync(ok, "DeleteCampaign", id.ToString(CultureInfo.InvariantCulture), error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "campaigns" });
    }

    public async Task<IActionResult> OnPostSaveHomeAsync(List<int>? courseIds)
    {
        var features = (courseIds ?? new List<int>())
            .Where(id => id > 0)
            .Select(id => ((string?)null, (int?)id))
            .ToList();
        var (ok, error) = await _operations.SetHomepageFeaturesAsync(features);
        await LogAsync(ok, "SaveHomepage", "features", error);
        Flash(ok, error);
        return RedirectToPage(new { tab = "home" });
    }

    /// <summary>Date-only inputs bind as Unspecified; Npgsql needs Utc for timestamptz.</summary>
    private static DateTime Normalize(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    private void Flash(bool ok, string? error)
    {
        TempData["Message"] = ok ? "Saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
    }

    private async Task LogAsync(bool ok, string action, string target, string? error)
    {
        await _logs.RecordAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.Identity?.Name ?? string.Empty,
            ok ? action : "AdminOperationFailed",
            "Operations",
            ok ? target : string.Empty,
            ok ? null : error,
            HttpContext.Connection.RemoteIpAddress?.ToString());
    }
}
