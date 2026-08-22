using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Gamification.Models;
using OpenLearning.Gamification.Services;

namespace OpenLearning.Web.Pages.Gamification;

[Authorize(Policy = Policies.RequireAdmin)]
public sealed class ManageModel : PageModel
{
    private readonly GamificationService _service;

    public ManageModel(GamificationService service)
    {
        _service = service;
    }

    public List<PointRule> Rules { get; private set; } = new();
    public List<BadgeDefinition> Badges { get; private set; } = new();
    public List<GamificationChallenge> Challenges { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostRuleAsync(string name, string eventType, int points, int dailyCap)
    {
        try
        {
            await _service.CreateRuleAsync(name, eventType, points, dailyCap);
            Flash("Point rule created.", true);
        }
        catch (ArgumentException ex)
        {
            Flash(ex.Message, false);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRuleStateAsync(int id, bool enabled)
    {
        var result = await _service.SetRuleEnabledAsync(id, enabled);
        Flash(result.Error ?? (enabled ? "Rule enabled." : "Rule disabled."), result.Ok);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBadgeAsync(string key, string name, int requiredPoints, bool publish)
    {
        try
        {
            await _service.CreateBadgeVersionAsync(key, name, requiredPoints, publish);
            Flash("Badge criteria version created.", true);
        }
        catch (ArgumentException ex)
        {
            Flash(ex.Message, false);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChallengeAsync(
        string name,
        DateTime startsAt,
        DateTime endsAt,
        int targetPoints,
        GamificationScopeKind scopeKind,
        string scopeId)
    {
        try
        {
            await _service.CreateChallengeAsync(name, startsAt, endsAt, targetPoints, scopeKind, scopeId);
            Flash("Challenge created.", true);
        }
        catch (ArgumentException ex)
        {
            Flash(ex.Message, false);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCorrectionAsync(int entryId, int correctedAmount, string reason)
    {
        var result = await _service.CorrectAsync(entryId, correctedAmount, reason);
        Flash(result.Error ?? "Compensating correction recorded.", result.Ok);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostModerationAsync(
        string userId,
        GamificationScopeKind scopeKind,
        string scopeId,
        bool hidden,
        string reason)
    {
        var result = await _service.ModerateAsync(userId, scopeKind, scopeId, hidden, reason);
        Flash(result.Error ?? "Leaderboard moderation updated.", result.Ok);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBackfillAsync(
        int ruleId,
        string events,
        GamificationScopeKind scopeKind,
        string scopeId,
        bool preview)
    {
        var parsed = ParseEvents(events);
        if (parsed.Count == 0)
        {
            Flash("Backfill requires userId|sourceKey rows.", false);
            return RedirectToPage();
        }
        if (preview)
        {
            var result = await _service.PreviewBackfillAsync(ruleId, parsed.Select(x => x.SourceKey).ToList());
            Flash($"Preview: {result.EligibleEvents} events, up to {result.EstimatedPoints} points.", true);
        }
        else
        {
            var result = await _service.RunBackfillAsync(ruleId, parsed, scopeKind, scopeId);
            Flash($"Backfill: {result.AwardedEvents} events, {result.AwardedPoints} points.", true);
        }
        return RedirectToPage();
    }

    private static List<(string UserId, string SourceKey)> ParseEvents(string rows)
    {
        return rows.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(row => row.Split('|', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && parts.All(value => value.Length > 0))
            .Select(parts => (UserId: parts[0], SourceKey: parts[1]))
            .Distinct()
            .ToList();
    }

    private async Task LoadAsync()
    {
        Rules = await _service.GetRulesAsync();
        Badges = await _service.GetBadgeDefinitionsAsync();
        Challenges = await _service.GetAllChallengesAsync();
    }

    private void Flash(string message, bool ok)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
