using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Credits.Models;
using OpenLearning.Credits.Services;

namespace OpenLearning.Web.Pages.Credits;

[Authorize(Policy = OpenLearning.Auth.Policies.RequireStudent)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "学分与毕业")]
public sealed class IndexModel(CreditService credits) : PageModel
{
    public List<CreditAward> Ledger { get; private set; } = [];
    public LearnerProgram? Assignment { get; private set; }
    public AuditResult Audit { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Ledger = await credits.GetLedgerAsync(studentId);
        Assignment = await credits.GetLearnerProgramAsync(studentId);
        Audit = await credits.EvaluateAsync(studentId);
    }
}
