using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.TA;

[Authorize(Policy = Policies.RequireTeachingAssistant)]
public class IndexModel : PageModel
{
    private readonly IClassAssignmentLookup _lookup;

    public IndexModel(IClassAssignmentLookup lookup)
    {
        _lookup = lookup;
    }

    public IReadOnlyList<int> ClassIds { get; set; } = Array.Empty<int>();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        ClassIds = await _lookup.ListAssignedClassIdsAsync(userId);
        return Page();
    }
}
