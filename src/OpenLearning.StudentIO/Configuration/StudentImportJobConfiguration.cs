using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.StudentIO.Models;

namespace OpenLearning.StudentIO.Configuration;

public class StudentImportJobConfiguration : IEntityTypeConfiguration<StudentImportJob>
{
    public void Configure(EntityTypeBuilder<StudentImportJob> builder)
    {
        builder.Property(j => j.UserId).HasMaxLength(450).IsRequired();
        builder.Property(j => j.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(j => j.ErrorFileKey).HasMaxLength(500);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => j.AsyncIOJobId).IsUnique();
    }
}

public class StudentImportRowErrorConfiguration : IEntityTypeConfiguration<StudentImportRowError>
{
    public void Configure(EntityTypeBuilder<StudentImportRowError> builder)
    {
        builder.Property(e => e.Field).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(e => e.JobId);
    }
}
