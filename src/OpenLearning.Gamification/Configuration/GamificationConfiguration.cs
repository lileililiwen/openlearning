using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Gamification.Models;

namespace OpenLearning.Gamification.Configuration;

public sealed class PointRuleConfiguration : IEntityTypeConfiguration<PointRule>
{
    public void Configure(EntityTypeBuilder<PointRule> builder)
    {
        builder.ToTable("GamificationPointRules");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.EventType, x.Version }).IsUnique();
    }
}

public sealed class GamificationPointEntryConfiguration : IEntityTypeConfiguration<GamificationPointEntry>
{
    public void Configure(EntityTypeBuilder<GamificationPointEntry> builder)
    {
        builder.ToTable("GamificationPointEntries");
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.SourceKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScopeId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.SourceKey).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.ScopeKind, x.ScopeId, x.CreatedAt });
        builder.HasOne(x => x.Rule).WithMany().HasForeignKey(x => x.PointRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CorrectsEntry).WithMany().HasForeignKey(x => x.CorrectsEntryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BadgeDefinitionConfiguration : IEntityTypeConfiguration<BadgeDefinition>
{
    public void Configure(EntityTypeBuilder<BadgeDefinition> builder)
    {
        builder.ToTable("GamificationBadges");
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.Key, x.CriteriaVersion }).IsUnique();
    }
}

public sealed class BadgeAwardConfiguration : IEntityTypeConfiguration<BadgeAward>
{
    public void Configure(EntityTypeBuilder<BadgeAward> builder)
    {
        builder.ToTable("GamificationBadgeAwards");
        builder.Property(x => x.BadgeKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Evidence).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.BadgeKey, x.UserId }).IsUnique();
        builder.HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GamificationChallengeConfiguration : IEntityTypeConfiguration<GamificationChallenge>
{
    public void Configure(EntityTypeBuilder<GamificationChallenge> builder)
    {
        builder.ToTable("GamificationChallenges");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ScopeId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.ScopeKind, x.ScopeId, x.StartsAt, x.EndsAt });
    }
}

public sealed class LeaderboardPreferenceConfiguration : IEntityTypeConfiguration<LeaderboardPreference>
{
    public void Configure(EntityTypeBuilder<LeaderboardPreference> builder)
    {
        builder.ToTable("GamificationLeaderboardPreferences");
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.DisplayAlias).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class LeaderboardModerationConfiguration : IEntityTypeConfiguration<LeaderboardModeration>
{
    public void Configure(EntityTypeBuilder<LeaderboardModeration> builder)
    {
        builder.ToTable("GamificationLeaderboardModeration");
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ScopeId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.ScopeKind, x.ScopeId }).IsUnique();
    }
}
