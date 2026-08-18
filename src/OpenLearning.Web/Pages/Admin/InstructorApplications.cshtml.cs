using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.UserManagement.Models;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class InstructorApplicationsModel : PageModel
{
    private readonly InstructorApplicationService _applications;

    public InstructorApplicationsModel(InstructorApplicationService applications)
    {
        _applications = applications;
    }

    public List<InstructorApplication> Pending { get; set; } = new();

    public List<InstructorApplication> Reviewed { get; set; } = new();

    public async Task OnGetAsync()
    {
        Pending = await _applications.GetPendingAsync();
        Reviewed = await _applications.GetReviewedAsync(20);
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _applications.ApproveAsync(id, reviewerId);
        return Flash(ok, error);
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string? reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _applications.RejectAsync(id, reviewerId, reason);
        return Flash(ok, error);
    }

    private IActionResult Flash(bool ok, string? error)
    {
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
        }
        else
        {
            TempData["Message"] = "Saved.";
            TempData["MessageType"] = "success";
        }

        return RedirectToPage();
    }
}
