using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Payments.Models;
using OpenLearning.Payments.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public sealed class PaymentsModel(PaymentService payments) : PageModel
{
    public List<PaymentIntent> Intents { get; private set; } = [];
    public List<PaymentReconciliationIssue> Issues { get; private set; } = [];
    public PaymentService.ProviderHealth? ProviderHealth { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostReconcileAsync(Guid id)
    {
        await payments.ReconcileAsync(id);
        TempData["Message"] = "Reconciliation completed; provider failures remain visible and retryable.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefundAsync(Guid id, decimal amount)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var (_, error) = await payments.RequestRefundAsync(id, amount, actor);
        TempData["Message"] = error ?? "Refund submitted to the provider; downstream effects wait for confirmation.";
        TempData["MessageType"] = error is null ? "success" : "danger";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Intents = await payments.GetRecentAsync();
        Issues = await payments.GetOpenIssuesAsync();
        ProviderHealth = await payments.GetProviderHealthAsync();
    }
}
