using Microsoft.EntityFrameworkCore;
using OpenLearning.Gamification.Models;

namespace OpenLearning.Gamification.Services;

public sealed record PointAwardResult(bool Created, int Amount, bool WasCapped, string? Error);
public sealed record LeaderboardRow(int Rank, string Alias, int Points);
public sealed record ChallengeProgress(GamificationChallenge Challenge, int Points, bool IsComplete);

public sealed class GamificationService
{
    private readonly DbContext _db;

    public GamificationService(DbContext db)
    {
        _db = db;
    }

    public async Task<PointRule> CreateRuleAsync(string name, string eventType, int points, int dailyCap)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Rule name and event type are required.");
        }
        if (points <= 0 || dailyCap <= 0)
        {
            throw new ArgumentException("Points and daily cap must be positive.");
        }

        var normalizedEvent = eventType.Trim().ToLowerInvariant();
        var version = await _db.Set<PointRule>()
            .Where(x => x.EventType == normalizedEvent)
            .MaxAsync(x => (int?)x.Version) ?? 0;
        var rule = new PointRule
        {
            Name = name.Trim(),
            EventType = normalizedEvent,
            Points = points,
            DailyCap = dailyCap,
            Version = version + 1
        };
        _db.Add(rule);
        await _db.SaveChangesAsync();
        return rule;
    }

    public async Task<(bool Ok, string? Error)> SetRuleEnabledAsync(int ruleId, bool enabled)
    {
        var rule = await _db.Set<PointRule>().SingleOrDefaultAsync(x => x.Id == ruleId);
        if (rule is null)
        {
            return (false, "Rule not found.");
        }
        rule.IsEnabled = enabled;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<PointAwardResult> AwardTrustedEventAsync(
        string userId,
        string eventType,
        string sourceKey,
        GamificationScopeKind scopeKind,
        string scopeId,
        DateTime? occurredAt = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sourceKey))
        {
            return new(false, 0, false, "User and source key are required.");
        }
        var normalizedEvent = eventType.Trim().ToLowerInvariant();
        var rule = await _db.Set<PointRule>()
            .Where(x => x.EventType == normalizedEvent && x.IsEnabled)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();
        if (rule is null)
        {
            return new(false, 0, false, "No enabled rule exists for this trusted event.");
        }
        if (await _db.Set<GamificationPointEntry>().AnyAsync(x => x.SourceKey == sourceKey))
        {
            return new(false, 0, false, null);
        }

        var timestamp = occurredAt ?? DateTime.UtcNow;
        var start = timestamp.Date;
        var end = start.AddDays(1);
        var awardedToday = await _db.Set<GamificationPointEntry>()
            .Where(x => x.UserId == userId && x.PointRuleId == rule.Id && x.CreatedAt >= start && x.CreatedAt < end)
            .SumAsync(x => x.Amount);
        var amount = Math.Max(0, Math.Min(rule.Points, rule.DailyCap - awardedToday));
        var entry = new GamificationPointEntry
        {
            UserId = userId,
            PointRuleId = rule.Id,
            RuleVersion = rule.Version,
            SourceKey = sourceKey.Trim(),
            EventType = normalizedEvent,
            RequestedPoints = rule.Points,
            Amount = amount,
            WasCapped = amount < rule.Points,
            ScopeKind = scopeKind,
            ScopeId = NormalizeScopeId(scopeKind, scopeId),
            CreatedAt = timestamp
        };
        _db.Add(entry);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(entry).State = EntityState.Detached;
            return new(false, 0, false, null);
        }
        await EvaluateBadgesAsync(userId);
        return new(true, amount, entry.WasCapped, null);
    }

    public async Task<(bool Ok, string? Error, GamificationPointEntry? Entry)> CorrectAsync(
        int entryId,
        int correctedAmount,
        string reason)
    {
        var original = await _db.Set<GamificationPointEntry>().SingleOrDefaultAsync(x => x.Id == entryId);
        if (original is null)
        {
            return (false, "Entry not found.", null);
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return (false, "Correction reason is required.", null);
        }
        if (await _db.Set<GamificationPointEntry>().AnyAsync(x => x.CorrectsEntryId == entryId))
        {
            return (false, "Entry has already been corrected.", null);
        }

        var correction = new GamificationPointEntry
        {
            UserId = original.UserId,
            PointRuleId = original.PointRuleId,
            RuleVersion = original.RuleVersion,
            SourceKey = $"correction:{entryId}",
            EventType = "admin.correction",
            RequestedPoints = correctedAmount - original.Amount,
            Amount = correctedAmount - original.Amount,
            CorrectsEntryId = original.Id,
            ScopeKind = original.ScopeKind,
            ScopeId = original.ScopeId
        };
        _db.Add(correction);
        await _db.SaveChangesAsync();
        return (true, null, correction);
    }

    public async Task<BadgeDefinition> CreateBadgeVersionAsync(string key, string name, int requiredPoints, bool publish)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name) || requiredPoints <= 0)
        {
            throw new ArgumentException("Badge key, name, and positive criteria are required.");
        }
        var normalizedKey = key.Trim().ToLowerInvariant();
        var version = await _db.Set<BadgeDefinition>()
            .Where(x => x.Key == normalizedKey)
            .MaxAsync(x => (int?)x.CriteriaVersion) ?? 0;
        if (publish)
        {
            var old = await _db.Set<BadgeDefinition>()
                .Where(x => x.Key == normalizedKey && x.IsPublished)
                .ToListAsync();
            foreach (var definition in old)
            {
                definition.IsPublished = false;
            }
        }
        var badge = new BadgeDefinition
        {
            Key = normalizedKey,
            Name = name.Trim(),
            RequiredPoints = requiredPoints,
            CriteriaVersion = version + 1,
            IsPublished = publish
        };
        _db.Add(badge);
        await _db.SaveChangesAsync();
        return badge;
    }

    public async Task<IReadOnlyList<BadgeAward>> EvaluateBadgesAsync(string userId)
    {
        var total = await _db.Set<GamificationPointEntry>()
            .Where(x => x.UserId == userId)
            .SumAsync(x => x.Amount);
        var awardedKeys = await _db.Set<BadgeAward>()
            .Where(x => x.UserId == userId)
            .Select(x => x.BadgeKey)
            .ToListAsync();
        var eligible = await _db.Set<BadgeDefinition>()
            .Where(x => x.IsPublished && x.IsEnabled && x.RequiredPoints <= total && !awardedKeys.Contains(x.Key))
            .ToListAsync();
        var awards = eligible.Select(x => new BadgeAward
        {
            BadgeDefinitionId = x.Id,
            BadgeKey = x.Key,
            UserId = userId,
            CriteriaVersion = x.CriteriaVersion,
            Evidence = $"total-points:{total};criteria:{x.RequiredPoints}"
        }).ToList();
        if (awards.Count > 0)
        {
            _db.AddRange(awards);
            await _db.SaveChangesAsync();
        }
        return awards;
    }

    public async Task<GamificationChallenge> CreateChallengeAsync(
        string name,
        DateTime startsAt,
        DateTime endsAt,
        int targetPoints,
        GamificationScopeKind scopeKind,
        string scopeId)
    {
        if (string.IsNullOrWhiteSpace(name) || endsAt <= startsAt || targetPoints <= 0)
        {
            throw new ArgumentException("Challenge name, valid dates, and positive target are required.");
        }
        var challenge = new GamificationChallenge
        {
            Name = name.Trim(),
            StartsAt = startsAt,
            EndsAt = endsAt,
            TargetPoints = targetPoints,
            ScopeKind = scopeKind,
            ScopeId = NormalizeScopeId(scopeKind, scopeId)
        };
        _db.Add(challenge);
        await _db.SaveChangesAsync();
        return challenge;
    }

    public async Task SetPreferenceAsync(string userId, bool visible, string alias)
    {
        if (visible && string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException("A display alias is required when joining leaderboards.");
        }
        var preference = await _db.Set<LeaderboardPreference>().SingleOrDefaultAsync(x => x.UserId == userId);
        if (preference is null)
        {
            preference = new LeaderboardPreference { UserId = userId };
            _db.Add(preference);
        }
        preference.IsVisible = visible;
        preference.DisplayAlias = visible ? alias.Trim() : string.Empty;
        preference.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<(bool Ok, string? Error)> ModerateAsync(
        string userId,
        GamificationScopeKind scopeKind,
        string scopeId,
        bool hidden,
        string reason)
    {
        var normalizedScope = NormalizeScopeId(scopeKind, scopeId);
        var record = await _db.Set<LeaderboardModeration>().SingleOrDefaultAsync(
            x => x.UserId == userId && x.ScopeKind == scopeKind && x.ScopeId == normalizedScope);
        if (record is null)
        {
            record = new LeaderboardModeration
            {
                UserId = userId,
                ScopeKind = scopeKind,
                ScopeId = normalizedScope
            };
            _db.Add(record);
        }
        record.IsHidden = hidden;
        record.Reason = reason.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, IReadOnlyList<LeaderboardRow> Rows)> GetLeaderboardAsync(
        GamificationScopeKind scopeKind,
        string scopeId,
        bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return (false, "Scope access denied.", Array.Empty<LeaderboardRow>());
        }
        var normalizedScope = NormalizeScopeId(scopeKind, scopeId);
        var preferences = await _db.Set<LeaderboardPreference>()
            .AsNoTracking()
            .Where(x => x.IsVisible)
            .ToDictionaryAsync(x => x.UserId);
        var hidden = await _db.Set<LeaderboardModeration>()
            .AsNoTracking()
            .Where(x => x.ScopeKind == scopeKind && x.ScopeId == normalizedScope && x.IsHidden)
            .Select(x => x.UserId)
            .ToListAsync();
        var points = await _db.Set<GamificationPointEntry>()
            .AsNoTracking()
            .Where(x => x.ScopeKind == scopeKind && x.ScopeId == normalizedScope)
            .GroupBy(x => x.UserId)
            .Select(x => new { UserId = x.Key, Points = x.Sum(entry => entry.Amount) })
            .Where(x => x.Points > 0)
            .ToListAsync();
        var ranked = points
            .Where(x => preferences.ContainsKey(x.UserId) && !hidden.Contains(x.UserId))
            .OrderByDescending(x => x.Points)
            .ThenBy(x => preferences[x.UserId].DisplayAlias)
            .ToList();
        var rows = new List<LeaderboardRow>();
        var rank = 0;
        int? previousPoints = null;
        foreach (var item in ranked)
        {
            if (previousPoints != item.Points)
            {
                rank++;
                previousPoints = item.Points;
            }
            rows.Add(new LeaderboardRow(rank, preferences[item.UserId].DisplayAlias, item.Points));
        }
        return (true, null, rows);
    }

    public async Task<IReadOnlyList<ChallengeProgress>> GetChallengesAsync(string userId, DateTime? now = null)
    {
        var timestamp = now ?? DateTime.UtcNow;
        var challenges = await _db.Set<GamificationChallenge>()
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.StartsAt <= timestamp && x.EndsAt >= timestamp)
            .OrderBy(x => x.EndsAt)
            .ToListAsync();
        var result = new List<ChallengeProgress>();
        foreach (var challenge in challenges)
        {
            var points = await _db.Set<GamificationPointEntry>()
                .Where(x => x.UserId == userId && x.ScopeKind == challenge.ScopeKind &&
                    x.ScopeId == challenge.ScopeId && x.CreatedAt >= challenge.StartsAt && x.CreatedAt <= challenge.EndsAt)
                .SumAsync(x => x.Amount);
            result.Add(new ChallengeProgress(challenge, points, points >= challenge.TargetPoints));
        }
        return result;
    }

    public Task<List<GamificationPointEntry>> GetHistoryAsync(string userId)
    {
        return _db.Set<GamificationPointEntry>()
            .AsNoTracking()
            .Include(x => x.Rule)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public Task<List<BadgeAward>> GetBadgesAsync(string userId)
    {
        return _db.Set<BadgeAward>()
            .AsNoTracking()
            .Include(x => x.Badge)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AwardedAt)
            .ToListAsync();
    }

    public Task<List<PointRule>> GetRulesAsync()
    {
        return _db.Set<PointRule>().AsNoTracking().OrderBy(x => x.EventType).ThenByDescending(x => x.Version).ToListAsync();
    }

    public Task<List<BadgeDefinition>> GetBadgeDefinitionsAsync()
    {
        return _db.Set<BadgeDefinition>().AsNoTracking().OrderBy(x => x.Key).ThenByDescending(x => x.CriteriaVersion).ToListAsync();
    }

    public Task<List<GamificationChallenge>> GetAllChallengesAsync()
    {
        return _db.Set<GamificationChallenge>().AsNoTracking().OrderByDescending(x => x.StartsAt).ToListAsync();
    }

    public async Task<(int EligibleEvents, int EstimatedPoints)> PreviewBackfillAsync(
        int ruleId,
        IReadOnlyCollection<string> sourceKeys)
    {
        var rule = await _db.Set<PointRule>().AsNoTracking().SingleAsync(x => x.Id == ruleId);
        var existing = await _db.Set<GamificationPointEntry>()
            .Where(x => sourceKeys.Contains(x.SourceKey))
            .Select(x => x.SourceKey)
            .ToListAsync();
        var eligible = sourceKeys.Distinct().Count(x => !existing.Contains(x));
        return (eligible, eligible * rule.Points);
    }

    public async Task<(int AwardedEvents, int AwardedPoints)> RunBackfillAsync(
        int ruleId,
        IReadOnlyCollection<(string UserId, string SourceKey)> events,
        GamificationScopeKind scopeKind,
        string scopeId)
    {
        var rule = await _db.Set<PointRule>().AsNoTracking().SingleAsync(x => x.Id == ruleId && x.IsEnabled);
        var awardedEvents = 0;
        var awardedPoints = 0;
        foreach (var item in events.Distinct())
        {
            var result = await AwardTrustedEventAsync(item.UserId, rule.EventType, item.SourceKey, scopeKind, scopeId);
            if (result.Created)
            {
                awardedEvents++;
                awardedPoints += result.Amount;
            }
        }
        return (awardedEvents, awardedPoints);
    }

    private static string NormalizeScopeId(GamificationScopeKind scopeKind, string scopeId)
    {
        if (scopeKind == GamificationScopeKind.Platform)
        {
            return "platform";
        }
        if (string.IsNullOrWhiteSpace(scopeId))
        {
            throw new ArgumentException("A scope identifier is required.");
        }
        return scopeId.Trim();
    }
}
