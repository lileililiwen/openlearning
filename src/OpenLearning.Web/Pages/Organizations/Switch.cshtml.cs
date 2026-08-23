using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Organizations.Models;
using OpenLearning.Organizations.Services;

namespace OpenLearning.Web.Pages.Organizations;

[Authorize]
public sealed class SwitchModel(OrganizationService organizations, IOrganizationContext context) : PageModel
{
    public List<OrganizationMembership> Memberships { get; private set; } = [];
    public async Task OnGetAsync()
    {
        Memberships = await organizations.MembershipsForUserAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
    public async Task<IActionResult> OnPostAsync(int organizationId) { if (!await context.SetActiveAsync(organizationId)) { return Forbid(); } return RedirectToPage("Manage"); }
    public IActionResult OnPostClear() { context.Clear(); return RedirectToPage("/Index"); }
}
