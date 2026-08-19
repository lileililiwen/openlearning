using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.Distributor;

[Authorize(Policy = Policies.RequireDistributor)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "分销工作台")]
public class IndexModel : PageModel
{
    private readonly DistributionService _distribution;

    public IndexModel(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public decimal Available { get; set; }

    public decimal TotalEarned { get; set; }

    public List<CommissionEntry> RecentCommissions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _distribution.EnsureProfileAsync(userId);
        Available = await _distribution.GetAvailableAsync(userId);
        TotalEarned = await _distribution.GetTotalEarnedAsync(userId);
        RecentCommissions = await _distribution.GetCommissionsAsync(userId, pageSize: 10);
    }
}
