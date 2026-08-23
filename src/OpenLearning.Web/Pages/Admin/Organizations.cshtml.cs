using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Organizations.Models;
using OpenLearning.Organizations.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public sealed class OrganizationsModel(OrganizationService organizations) : PageModel
{
    public List<Organization> Items { get; private set; } = [];
    public Dictionary<int, List<OrganizationAudit>> Audits { get; private set; } = [];
    public async Task OnGetAsync()
    {
        Items = await organizations.ListAsync();
        var audits = await Task.WhenAll(Items.Select(async item =>
            (item.Id, Entries: await organizations.AuditsAsync(item.Id))));
        Audits = audits.ToDictionary(x => x.Id, x => x.Entries);
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string slug) { await organizations.CreateAsync(name, slug, Actor()); return RedirectToPage(); }
    public async Task<IActionResult> OnPostStatusAsync(int id, OrganizationStatus status) { await organizations.SetStatusAsync(id, status, Actor()); return RedirectToPage(); }
    public async Task<IActionResult> OnPostConfigureAsync(int id, string name, string primaryColor, int maximumDepartmentDepth) { await organizations.ConfigurePlatformAsync(id, name, primaryColor, maximumDepartmentDepth, Actor()); return RedirectToPage(); }
    private string Actor()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
