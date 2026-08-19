using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;

namespace OpenLearning.Web.Pages.TA;

[Authorize(Policy = Policies.RequireTeachingAssistant)]
public class RosterModel : PageModel
{
    private readonly IClassAssignmentLookup _lookup;
    private readonly ClassGroupService _classes;
    private readonly ClassRosterService _roster;

    public RosterModel(IClassAssignmentLookup lookup, ClassGroupService classes, ClassRosterService roster)
    {
        _lookup = lookup;
        _classes = classes;
        _roster = roster;
    }

    public ClassGroup? ClassGroup { get; set; }

    public List<ClassRosterRow> Rows { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int classId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, classId))
        {
            return Forbid();
        }

        ClassGroup = await _classes.GetByIdAsync(classId);
        if (ClassGroup is null)
        {
            return NotFound();
        }

        Rows = await _roster.GetRosterAsync(classId);
        return Page();
    }
}
