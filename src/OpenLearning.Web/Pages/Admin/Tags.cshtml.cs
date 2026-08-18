using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Logging.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class TagsModel : PageModel
{
    private readonly TagService _tags;
    private readonly LogService _logs;

    public TagsModel(TagService tags, LogService logs)
    {
        _tags = tags;
        _logs = logs;
    }

    public List<Tag> Tags { get; set; } = new();

    public Dictionary<int, int> Counts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tags = await _tags.GetAllAsync();
        Counts = await _tags.GetCourseCountsAsync();
    }

    public async Task<IActionResult> OnPostRenameAsync(int id, string name)
    {
        var (ok, error) = await _tags.RenameAsync(id, name);
        await LogAsync(ok, "RenameTag", id, error);
        Flash(ok, error);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMergeAsync(int id, int targetId)
    {
        var (ok, error) = await _tags.MergeAsync(id, targetId);
        await LogAsync(ok, "MergeTag", id, error);
        Flash(ok, error);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRetireAsync(int id)
    {
        var (ok, error) = await _tags.RetireAsync(id);
        await LogAsync(ok, "RetireTag", id, error);
        Flash(ok, error);
        return RedirectToPage();
    }

    private void Flash(bool ok, string? error)
    {
        TempData["Message"] = ok ? "Saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
    }

    private async Task LogAsync(bool ok, string action, int id, string? error)
    {
        await _logs.RecordAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.Identity?.Name ?? string.Empty,
            ok ? action : "AdminOperationFailed",
            "Tag",
            ok ? id.ToString(CultureInfo.InvariantCulture) : string.Empty,
            ok ? null : error,
            HttpContext.Connection.RemoteIpAddress?.ToString());
    }
}
