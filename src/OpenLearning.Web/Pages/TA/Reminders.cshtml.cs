using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.TA;

[Authorize(Policy = Policies.RequireTeachingAssistant)]
public class RemindersModel : PageModel
{
    private readonly IClassAssignmentLookup _lookup;

    public RemindersModel(IClassAssignmentLookup lookup)
    {
        _lookup = lookup;
    }

    public int ClassId { get; set; }

    public async Task<IActionResult> OnGetAsync(int classId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, classId))
        {
            return Forbid();
        }

        ClassId = classId;
        return Page();
    }
}
