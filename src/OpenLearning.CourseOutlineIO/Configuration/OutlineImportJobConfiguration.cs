using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.CourseOutlineIO.Models;

namespace OpenLearning.CourseOutlineIO.Configuration;

public class OutlineImportJobConfiguration : IEntityTypeConfiguration<OutlineImportJob>
{
    public void Configure(EntityTypeBuilder<OutlineImportJob> builder)
    {
        builder.Property(j => j.UserId).HasMaxLength(450).IsRequired();
        builder.Property(j => j.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(j => j.ErrorFileKey).HasMaxLength(500);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => j.AsyncIOJobId).IsUnique();
    }
}

public class OutlineRowErrorConfiguration : IEntityTypeConfiguration<OutlineRowError>
{
    public void Configure(EntityTypeBuilder<OutlineRowError> builder)
    {
        builder.Property(e => e.Field).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(e => e.JobId);
    }
}
