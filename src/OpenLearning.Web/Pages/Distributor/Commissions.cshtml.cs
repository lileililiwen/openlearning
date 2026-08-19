using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.Distributor;

[Authorize(Policy = Policies.RequireDistributor)]
public class CommissionsModel : PageModel
{
    private readonly DistributionService _distribution;

    public CommissionsModel(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public List<CommissionEntry> Commissions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public CommissionStatus? Status { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Commissions = await _distribution.GetCommissionsAsync(userId, Status, page);
    }
}
