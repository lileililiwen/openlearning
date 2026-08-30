using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Authoring.Models;

namespace OpenLearning.Authoring.Configuration;

public sealed class CourseRevisionConfiguration : IEntityTypeConfiguration<CourseRevision>
{
    public void Configure(EntityTypeBuilder<CourseRevision> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.OwnerId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Summary).HasMaxLength(2000);
        builder.Property(r => r.Level).HasMaxLength(50);
        builder.Property(r => r.Language).HasMaxLength(20);
        builder.Property(r => r.PublishedBy).HasMaxLength(450);
        builder.HasIndex(r => new { r.CourseId, r.State });
        builder.HasIndex(r => r.CourseId);
    }
}

public sealed class CourseRevisionPointerConfiguration : IEntityTypeConfiguration<CourseRevisionPointer>
{
    public void Configure(EntityTypeBuilder<CourseRevisionPointer> builder)
    {
        builder.HasKey(p => p.CourseId);
        builder.Property(p => p.OwnerId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.RowVersion).IsConcurrencyToken();
        builder.HasIndex(p => p.DraftRevisionId);
        builder.HasIndex(p => p.ActiveRevisionId);
    }
}

public sealed class RevisionValidationResultConfiguration : IEntityTypeConfiguration<RevisionValidationResult>
{
    public void Configure(EntityTypeBuilder<RevisionValidationResult> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.CheckedBy).HasMaxLength(450).IsRequired();
        builder.HasIndex(v => new { v.CourseId, v.RevisionId });
    }
}

public sealed class RevisionAuditConfiguration : IEntityTypeConfiguration<RevisionAudit>
{
    public void Configure(EntityTypeBuilder<RevisionAudit> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ActorId).HasMaxLength(450).IsRequired();
        builder.HasIndex(a => a.CourseId);
        builder.HasIndex(a => a.At);
    }
}
