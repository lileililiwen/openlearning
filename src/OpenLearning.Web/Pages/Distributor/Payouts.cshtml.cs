using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.Distributor;

[Authorize(Policy = Policies.RequireDistributor)]
public class PayoutsModel : PageModel
{
    private readonly DistributionService _distribution;

    public PayoutsModel(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public decimal Available { get; set; }

    public List<PayoutRequest> Payouts { get; set; } = new();

    [BindProperty]
    public decimal Amount { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Available = await _distribution.GetAvailableAsync(userId);
        Payouts = await _distribution.GetPayoutsAsync(userId);
    }

    public async Task<IActionResult> OnPostRequestAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _distribution.RequestPayoutAsync(userId, Amount);
        TempData["Message"] = ok ? "Payout request submitted for review." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
