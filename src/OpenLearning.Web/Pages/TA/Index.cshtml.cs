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
public class IndexModel : PageModel
{
    private readonly IClassAssignmentLookup _lookup;
    private readonly ClassGroupService _classes;

    public IndexModel(IClassAssignmentLookup lookup, ClassGroupService classes)
    {
        _lookup = lookup;
        _classes = classes;
    }

    public List<ClassGroup> Classes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ids = await _lookup.ListAssignedClassIdsAsync(userId);
        Classes = new List<ClassGroup>();
        foreach (var id in ids)
        {
            var classGroup = await _classes.GetByIdAsync(id);
            if (classGroup is not null)
            {
                Classes.Add(classGroup);
            }
        }

        Classes = Classes.OrderByDescending(c => c.EndsAt).ToList();
        return Page();
    }
}
