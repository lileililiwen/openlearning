using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Memberships.Models;
using OpenLearning.Memberships.Services;

namespace OpenLearning.Web.Pages.Memberships;

public class IndexModel : PageModel
{
    private readonly MembershipService _memberships;

    public IndexModel(MembershipService memberships)
    {
        _memberships = memberships;
    }

    public List<MembershipPlan> Plans { get; set; } = new();

    public Membership? ActiveMembership { get; set; }

    public async Task OnGetAsync()
    {
        Plans = await _memberships.GetPlansAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            ActiveMembership = await _memberships.GetActiveAsync(userId);
        }
    }

    public async Task<IActionResult> OnPostPurchaseAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _memberships.PurchaseAsync(userId, id);
        TempData["Message"] = ok ? "Membership activated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
