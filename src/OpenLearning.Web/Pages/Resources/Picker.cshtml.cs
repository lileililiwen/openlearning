using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.ResourceCenter.Services;
using OpenLearning.Storage.Models;

namespace OpenLearning.Web.Pages.Resources;

/// <summary>
/// Server-rendered resource picker, loaded inside a modal iframe by forms with
/// URL fields. On selection it fills the caller's target input via
/// <c>parent.document.getElementById(target)</c> and closes the modal.
/// </summary>
[Authorize]
public class PickerModel : PageModel
{
    private readonly ResourceService _resources;

    public PickerModel(ResourceService resources)
    {
        _resources = resources;
    }

    public List<ResourceRow> Items { get; set; } = new();

    public int Total { get; set; }

    public bool IsAdmin { get; set; }

    [BindProperty(SupportsGet = true)]
    public FilePurpose? Purpose { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Id of the caller page's input to fill on selection.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Target { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsAdmin = User.IsInRole(Roles.Admin);
        var (items, total) = await _resources.ListAsync(
            userId, IsAdmin, Purpose, Search, Math.Max(1, PageNumber));
        Items = items;
        Total = total;
    }

    public static int PageCount(int total)
    {
        return Math.Max(1, (int)Math.Ceiling(total / (double)ResourceService.PageSize));
    }
}
