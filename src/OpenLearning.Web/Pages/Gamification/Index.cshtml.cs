using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Gamification.Models;
using OpenLearning.Gamification.Services;

namespace OpenLearning.Web.Pages.Gamification;

[Authorize(Policy = Policies.RequireStudent)]
public sealed class IndexModel : PageModel
{
    private readonly GamificationService _service;

    public IndexModel(GamificationService service)
    {
        _service = service;
    }

    public List<GamificationPointEntry> History { get; private set; } = new();
    public List<BadgeAward> Badges { get; private set; } = new();
    public IReadOnlyList<ChallengeProgress> Challenges { get; private set; } = Array.Empty<ChallengeProgress>();
    public IReadOnlyList<LeaderboardRow> Leaderboard { get; private set; } = Array.Empty<LeaderboardRow>();

    public async Task OnGetAsync()
    {
        History = await _service.GetHistoryAsync(UserId);
        Badges = await _service.GetBadgesAsync(UserId);
        Challenges = await _service.GetChallengesAsync(UserId);
        var board = await _service.GetLeaderboardAsync(GamificationScopeKind.Platform, string.Empty, true);
        Leaderboard = board.Rows;
    }

    public async Task<IActionResult> OnPostPreferenceAsync(bool visible, string? alias)
    {
        try
        {
            await _service.SetPreferenceAsync(UserId, visible, alias ?? string.Empty);
            TempData["Message"] = visible ? "Leaderboard visibility enabled." : "Leaderboard visibility disabled.";
            TempData["MessageType"] = "success";
        }
        catch (ArgumentException ex)
        {
            TempData["Message"] = ex.Message;
            TempData["MessageType"] = "danger";
        }
        return RedirectToPage();
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
