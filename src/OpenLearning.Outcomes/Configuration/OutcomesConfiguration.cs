using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Outcomes.Models;

namespace OpenLearning.Outcomes.Configuration;

public sealed class CourseOutcomeConfiguration : IEntityTypeConfiguration<CourseOutcome>
{
    public void Configure(EntityTypeBuilder<CourseOutcome> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OwnerId).HasMaxLength(450).IsRequired();
        builder.Property(o => o.Title).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(2000);
        builder.HasIndex(o => o.CourseId);
        builder.HasIndex(o => new { o.CourseId, o.OwnerId });
    }
}

public sealed class OutcomeActivityMappingConfiguration : IEntityTypeConfiguration<OutcomeActivityMapping>
{
    public void Configure(EntityTypeBuilder<OutcomeActivityMapping> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ActivityType).HasMaxLength(50).IsRequired();
        builder.HasIndex(m => new { m.CourseId, m.OutcomeId });
        // One mapping per activity within an outcome.
        builder.HasIndex(m => new { m.OutcomeId, m.ActivityId }).IsUnique();
    }
}

public sealed class MasteryResultConfiguration : IEntityTypeConfiguration<MasteryResult>
{
    public void Configure(EntityTypeBuilder<MasteryResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.LearnerId).HasMaxLength(450).IsRequired();
        builder.HasIndex(r => new { r.CourseId, r.OutcomeId, r.LearnerId }).IsUnique();
        builder.HasIndex(r => new { r.CourseId, r.LearnerId });
    }
}
