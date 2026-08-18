using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Logging.Services;
using OpenLearning.Memberships.Models;
using OpenLearning.Memberships.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class MembershipPlansModel : PageModel
{
    private readonly MembershipService _memberships;
    private readonly LogService _logs;

    public MembershipPlansModel(MembershipService memberships, LogService logs)
    {
        _memberships = memberships;
        _logs = logs;
    }

    public List<MembershipPlan> Plans { get; set; } = new();

    public async Task OnGetAsync()
    {
        Plans = await _memberships.GetAllPlansAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string description, decimal price, int durationDays)
    {
        var (ok, error) = await _memberships.CreatePlanAsync(name, description, price, durationDays);
        if (ok)
        {
            await _logs.RecordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                User.Identity?.Name ?? string.Empty,
                "CreateMembershipPlan",
                "MembershipPlan",
                name,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        Flash(ok, error);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var plan = await _memberships.GetPlanByIdAsync(id);
        if (plan is null)
        {
            return NotFound();
        }

        var (ok, error) = await _memberships.SetPlanActiveAsync(id, !plan.IsActive);
        if (ok)
        {
            await _logs.RecordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                User.Identity?.Name ?? string.Empty,
                "ToggleMembershipPlan",
                "MembershipPlan",
                id.ToString(CultureInfo.InvariantCulture),
                plan.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        Flash(ok, error);
        return RedirectToPage();
    }

    private void Flash(bool ok, string? error)
    {
        TempData["Message"] = ok ? "Saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
