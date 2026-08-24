using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Analytics.Models;

namespace OpenLearning.Analytics.Configuration;

public class LearningEventConfiguration : IEntityTypeConfiguration<LearningEvent>
{
    public void Configure(EntityTypeBuilder<LearningEvent> builder)
    {
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ActorKey).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PropertiesJson).HasMaxLength(8000);
        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => new { e.EventType, e.OccurredAt });
        builder.HasIndex(e => new { e.CourseId, e.OccurredAt });
        builder.HasIndex(e => e.ReceivedAt);
    }
}

public class RefreshRunConfiguration : IEntityTypeConfiguration<RefreshRun>
{
    public void Configure(EntityTypeBuilder<RefreshRun> builder)
    {
        builder.Property(r => r.Scope).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => new { r.Scope, r.Status });
    }
}

public class CourseFunnelAggregateConfiguration : IEntityTypeConfiguration<CourseFunnelAggregate>
{
    public void Configure(EntityTypeBuilder<CourseFunnelAggregate> builder)
    {
        builder.HasIndex(a => new { a.RefreshRunId, a.CourseId, a.Date });
    }
}

public class EngagementAggregateConfiguration : IEntityTypeConfiguration<EngagementAggregate>
{
    public void Configure(EntityTypeBuilder<EngagementAggregate> builder)
    {
        builder.HasIndex(a => new { a.RefreshRunId, a.CourseId, a.Date });
    }
}

public class CohortRetentionAggregateConfiguration : IEntityTypeConfiguration<CohortRetentionAggregate>
{
    public void Configure(EntityTypeBuilder<CohortRetentionAggregate> builder)
    {
        builder.HasIndex(a => new { a.RefreshRunId, a.CourseId, a.ClassGroupId });
    }
}

public class AssessmentAggregateConfiguration : IEntityTypeConfiguration<AssessmentAggregate>
{
    public void Configure(EntityTypeBuilder<AssessmentAggregate> builder)
    {
        builder.HasIndex(a => new { a.RefreshRunId, a.AssessmentId, a.Date });
    }
}

public class WorkloadAggregateConfiguration : IEntityTypeConfiguration<WorkloadAggregate>
{
    public void Configure(EntityTypeBuilder<WorkloadAggregate> builder)
    {
        builder.HasIndex(a => new { a.RefreshRunId, a.CourseId, a.Date });
    }
}

public class ExportAuditConfiguration : IEntityTypeConfiguration<ExportAudit>
{
    public void Configure(EntityTypeBuilder<ExportAudit> builder)
    {
        builder.Property(a => a.RequesterId).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Scope).HasMaxLength(50).IsRequired();
        builder.Property(a => a.FiltersJson).HasMaxLength(8000);
        builder.HasIndex(a => new { a.RequesterId, a.ExportedAt });
    }
}

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.Property(p => p.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Key).IsUnique();
    }
}
