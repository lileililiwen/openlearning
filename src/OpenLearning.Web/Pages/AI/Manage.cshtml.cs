using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AI.Models;
using OpenLearning.AI.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.AI;

[Authorize(Policy = OpenLearning.Auth.Policies.RequireAdmin)]
public sealed class ManageModel : PageModel
{
    private readonly AiLearningService _ai;
    public ManageModel(AiLearningService ai)
    {
        _ai = ai;
    }

    public List<AiPolicy> Policies { get; private set; } = new();
    public async Task OnGetAsync()
    {
        Policies = await _ai.PoliciesAsync();
    }

    public async Task<IActionResult> OnPostPolicyAsync(int? courseId, string provider, string model, string? secretReference,
        bool questions, bool drafts, bool grading, int quota, int retentionDays, int timeoutSeconds, decimal cost, string disclosure)
    {
        try
        {
            await _ai.ConfigureAsync(courseId, provider, model, secretReference ?? string.Empty, questions, drafts, grading,
                quota, retentionDays, timeoutSeconds, cost, disclosure);
            TempData["Success"] = "AI policy saved.";
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }
}
