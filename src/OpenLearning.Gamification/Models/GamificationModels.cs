namespace OpenLearning.Gamification.Models;

public enum GamificationScopeKind
{
    Platform,
    Organization,
    Course,
    Challenge
}

public sealed class PointRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public int Points { get; set; }
    public int DailyCap { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GamificationPointEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PointRuleId { get; set; }
    public PointRule? Rule { get; set; }
    public int RuleVersion { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int RequestedPoints { get; set; }
    public int Amount { get; set; }
    public bool WasCapped { get; set; }
    public int? CorrectsEntryId { get; set; }
    public GamificationPointEntry? CorrectsEntry { get; set; }
    public GamificationScopeKind ScopeKind { get; set; }
    public string ScopeId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BadgeDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CriteriaVersion { get; set; } = 1;
    public int RequiredPoints { get; set; }
    public bool IsPublished { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class BadgeAward
{
    public int Id { get; set; }
    public int BadgeDefinitionId { get; set; }
    public BadgeDefinition? Badge { get; set; }
    public string BadgeKey { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int CriteriaVersion { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

public sealed class GamificationChallenge
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int TargetPoints { get; set; }
    public GamificationScopeKind ScopeKind { get; set; }
    public string ScopeId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public sealed class LeaderboardPreference
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public string DisplayAlias { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LeaderboardModeration
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public GamificationScopeKind ScopeKind { get; set; }
    public string ScopeId { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public string Reason { get; set; } = string.Empty;
}
