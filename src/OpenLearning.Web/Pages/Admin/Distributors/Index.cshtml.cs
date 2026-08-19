using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.Admin.Distributors;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "分销管理")]
public class IndexModel : PageModel
{
    private readonly DistributionService _distribution;

    public IndexModel(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public List<DistributorProfile> Distributors { get; set; } = new();

    public async Task OnGetAsync()
    {
        Distributors = await _distribution.GetProfilesAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(string userId)
    {
        await _distribution.SetActiveAsync(userId, !(Distributors.FirstOrDefault(d => d.UserId == userId)?.IsActive ?? true));
        return RedirectToPage();
    }
}
