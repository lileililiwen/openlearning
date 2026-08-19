using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Settlement.Models;
using OpenLearning.Settlement.Services;

namespace OpenLearning.Web.Pages.Instructor;

[Authorize(Policy = Policies.RequireInstructor)]
public class RevenueModel : PageModel
{
    private readonly SettlementService _settlement;

    public RevenueModel(SettlementService settlement)
    {
        _settlement = settlement;
    }

    public decimal Total { get; set; }

    public decimal Available { get; set; }

    public List<(int CourseId, string Title, decimal Amount)> PerCourse { get; set; } = new();

    public List<(string Period, decimal Amount)> PerPeriod { get; set; } = new();

    public List<SettlementLedger> Ledger { get; set; } = new();

    public List<WithdrawalRequest> Withdrawals { get; set; } = new();

    public static decimal MinWithdrawal => SettlementService.MinWithdrawalAmount;

    [BindProperty]
    public decimal Amount { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostWithdrawAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _settlement.RequestWithdrawalAsync(userId, Amount);
        TempData["Message"] = ok ? "Withdrawal requested." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Total = await _settlement.GetTotalAsync(userId);
        Available = await _settlement.GetAvailableAsync(userId);
        PerCourse = await _settlement.GetPerCourseAsync(userId);
        PerPeriod = await _settlement.GetPerPeriodAsync(userId);
        Ledger = await _settlement.GetLedgerAsync(userId);
        Withdrawals = await _settlement.GetWithdrawalsAsync(userId);
    }
}
