using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.Admin.Distributors;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "分销管理", "结算报表")]
public class SettlementsModel : PageModel
{
    private readonly DistributionService _distribution;

    public SettlementsModel(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public List<DistributorSettlementStatement> Statements { get; set; } = new();

    public async Task OnGetAsync()
    {
        Statements = await _distribution.GetStatementsAsync();
    }
}
