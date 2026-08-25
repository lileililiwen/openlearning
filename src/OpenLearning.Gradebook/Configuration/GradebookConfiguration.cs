using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Gradebook.Models;

namespace OpenLearning.Gradebook.Configuration;

public class GradebookConfigConfiguration : IEntityTypeConfiguration<GradebookConfig>
{
    public void Configure(EntityTypeBuilder<GradebookConfig> builder)
    {
        builder.Property(c => c.PublishedBy).HasMaxLength(450);
        builder.HasIndex(c => c.CourseId).IsUnique();
        builder.HasMany(c => c.Items)
            .WithOne(i => i.Config)
            .HasForeignKey(i => i.ConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GradebookItemConfiguration : IEntityTypeConfiguration<GradebookItem>
{
    public void Configure(EntityTypeBuilder<GradebookItem> builder)
    {
        builder.HasIndex(i => new { i.ConfigId, i.Kind, i.SourceId }).IsUnique();
        builder.HasIndex(i => new { i.ConfigId, i.SortOrder });
    }
}

public class GradebookAdjustmentConfiguration : IEntityTypeConfiguration<GradebookAdjustment>
{
    public void Configure(EntityTypeBuilder<GradebookAdjustment> builder)
    {
        builder.Property(a => a.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.CreatedBy).HasMaxLength(450).IsRequired();
        builder.HasIndex(a => new { a.ItemId, a.StudentId }).IsUnique();
    }
}

public class GradebookSnapshotConfiguration : IEntityTypeConfiguration<GradebookSnapshot>
{
    public void Configure(EntityTypeBuilder<GradebookSnapshot> builder)
    {
        builder.Property(s => s.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(s => s.BasisJson).HasMaxLength(4000).IsRequired();
        builder.HasIndex(s => new { s.ConfigId, s.StudentId }).IsUnique();
    }
}
