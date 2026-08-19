using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Settlement.Models;
using OpenLearning.Settlement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class WithdrawalsModel : PageModel
{
    private readonly SettlementService _settlement;
    private readonly UserService _users;
    private readonly NotificationService _notifications;

    public WithdrawalsModel(SettlementService settlement, UserService users, NotificationService notifications)
    {
        _settlement = settlement;
        _users = users;
        _notifications = notifications;
    }

    public List<WithdrawalRequest> Requests { get; set; } = new();

    public Dictionary<string, string> InstructorNames { get; set; } = new();

    public async Task OnGetAsync()
    {
        Requests = await _settlement.ListPendingAsync();
        var ids = Requests.Select(r => r.InstructorId).Distinct().ToList();
        var users = await _users.GetByIdsAsync(ids);
        InstructorNames = users
            .Where(u => u is not null)
            .ToDictionary(u => u!.Id, u => u!.DisplayName);
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var request = await _settlement.GetByIdAsync(id);
        var (ok, error) = await _settlement.ReviewAsync(id, approve: true, adminId);
        if (ok && request is not null)
        {
            await _notifications.CreateAsync(
                request.InstructorId,
                NotificationType.Order,
                "Withdrawal paid",
                $"Your withdrawal request for {request.Amount.ToString("C", CultureInfo.InvariantCulture)} was paid.",
                "/Instructor/Revenue");
        }

        TempData["Message"] = ok ? "Withdrawal marked as paid." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var request = await _settlement.GetByIdAsync(id);
        var (ok, error) = await _settlement.ReviewAsync(id, approve: false, adminId);
        if (ok && request is not null)
        {
            await _notifications.CreateAsync(
                request.InstructorId,
                NotificationType.Order,
                "Withdrawal rejected",
                $"Your withdrawal request for {request.Amount.ToString("C", CultureInfo.InvariantCulture)} was rejected.",
                "/Instructor/Revenue");
        }

        TempData["Message"] = ok ? "Withdrawal rejected." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
