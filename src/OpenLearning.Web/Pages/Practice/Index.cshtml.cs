using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Practice;

[Authorize(Policy = Policies.RequireStudent)]
public class IndexModel : PageModel
{
    private readonly IncorrectAnswerService _incorrect;

    public IndexModel(IncorrectAnswerService incorrect)
    {
        _incorrect = incorrect;
    }

    public List<IncorrectAnswer> Entries { get; set; } = new();

    public HashSet<int> BookmarkedIds { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool UnresolvedOnly { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public bool BookmarkedOnly { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Entries = await _incorrect.ListAsync(userId, UnresolvedOnly, BookmarkedOnly);
        BookmarkedIds = await _incorrect.GetBookmarkedIdsAsync(userId, Entries.Select(e => e.QuestionId));
        return Page();
    }

    public async Task<IActionResult> OnPostToggleBookmarkAsync(int questionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _incorrect.ToggleBookmarkAsync(userId, questionId);
        return RedirectToPage(new { UnresolvedOnly, BookmarkedOnly });
    }
}
