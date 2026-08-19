using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Admin.Students;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public class ImportJobsModel : PageModel
{
    private readonly AsyncIOService _asyncIO;

    public ImportJobsModel(AsyncIOService asyncIO)
    {
        _asyncIO = asyncIO;
    }

    public List<AsyncIOJob> Items { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Items = await _asyncIO.ListJobsAsync(userId, isAdmin: true, kind: "student-import", page: Math.Max(1, PageNumber), pageSize: 20);
    }
}
