using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;
using OpenLearning.Logging.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Admin.Distributors;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "分销管理", "提现审核")]
public class PayoutsModel : PageModel
{
    private readonly DistributionService _distribution;
    private readonly LogService _logs;
    private readonly NotificationService _notifications;

    public PayoutsModel(DistributionService distribution, LogService logs, NotificationService notifications)
    {
        _distribution = distribution;
        _logs = logs;
        _notifications = notifications;
    }

    public List<PayoutRequest> Payouts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Payouts = await _distribution.ListPendingPayoutsAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var (ok, error) = await _distribution.ApprovePayoutAsync(id);
        if (ok)
        {
            var payout = (await _distribution.ListPendingPayoutsAsync()).FirstOrDefault(p => p.Id == id);
            if (payout is not null)
            {
                await _notifications.CreateAsync(
                    payout.DistributorUserId,
                    NotificationType.Order,
                    "Payout approved",
                    $"Your payout of {payout.Amount.ToString("C", CultureInfo.InvariantCulture)} was approved.",
                    "/Distributor/Payouts");
            }

            await _logs.RecordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.Identity?.Name ?? string.Empty,
                "ApprovePayout",
                "PayoutRequest",
                id.ToString(CultureInfo.InvariantCulture),
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Payout approved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        var (ok, error) = await _distribution.RejectPayoutAsync(id, "rejected by admin");
        if (ok)
        {
            var payout = (await _distribution.ListPendingPayoutsAsync()).FirstOrDefault(p => p.Id == id);
            if (payout is not null)
            {
                await _notifications.CreateAsync(
                    payout.DistributorUserId,
                    NotificationType.Order,
                    "Payout rejected",
                    $"Your payout of {payout.Amount.ToString("C", CultureInfo.InvariantCulture)} was not approved.",
                    "/Distributor/Payouts");
            }
        }

        TempData["Message"] = ok ? "Payout rejected." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
