using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Competency.Models;

namespace OpenLearning.Competency.Configuration;

public class CompetencyFrameworkConfiguration : IEntityTypeConfiguration<CompetencyFramework>
{
    public void Configure(EntityTypeBuilder<CompetencyFramework> builder)
    {
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(2000);
        builder.HasIndex(f => f.IsArchived);
        builder.HasMany(f => f.ScaleLevels)
            .WithOne(l => l.Framework)
            .HasForeignKey(l => l.FrameworkId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(f => f.Competencies)
            .WithOne(c => c.Framework)
            .HasForeignKey(c => c.FrameworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FrameworkScaleLevelConfiguration : IEntityTypeConfiguration<FrameworkScaleLevel>
{
    public void Configure(EntityTypeBuilder<FrameworkScaleLevel> builder)
    {
        builder.Property(l => l.Label).HasMaxLength(100).IsRequired();
        builder.HasIndex(l => new { l.FrameworkId, l.SortOrder }).IsUnique();
    }
}

public class CompetencyNodeConfiguration : IEntityTypeConfiguration<CompetencyNode>
{
    public void Configure(EntityTypeBuilder<CompetencyNode> builder)
    {
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.HasIndex(c => new { c.FrameworkId, c.SortOrder });
        builder.HasIndex(c => c.ParentId);
    }
}

public class ActivityMappingConfiguration : IEntityTypeConfiguration<ActivityMapping>
{
    public void Configure(EntityTypeBuilder<ActivityMapping> builder)
    {
        builder.Property(m => m.CreatedBy).HasMaxLength(450).IsRequired();
        builder.HasIndex(m => m.CompetencyId);
        builder.HasIndex(m => m.CourseId);
        builder.HasIndex(m => m.AssignmentId);
    }
}

public class CompetencyEvidenceConfiguration : IEntityTypeConfiguration<CompetencyEvidence>
{
    public void Configure(EntityTypeBuilder<CompetencyEvidence> builder)
    {
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.SourceKey).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CompetencyTitleSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.AttachmentUrl).HasMaxLength(1000);
        builder.Property(e => e.ReviewerId).HasMaxLength(450);
        builder.Property(e => e.ReviewReason).HasMaxLength(1000);
        builder.HasIndex(e => new { e.CompetencyId, e.SourceKey }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.Status });
    }
}
