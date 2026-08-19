using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.AsyncIO.Models;

namespace OpenLearning.AsyncIO.Configuration;

public class AsyncIOJobConfiguration : IEntityTypeConfiguration<AsyncIOJob>
{
    public void Configure(EntityTypeBuilder<AsyncIOJob> builder)
    {
        builder.Property(j => j.Kind).HasMaxLength(100).IsRequired();
        builder.Property(j => j.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(j => j.ResultFileKey).HasMaxLength(500);
        builder.Property(j => j.ErrorFileKey).HasMaxLength(500);
        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => new { j.Kind, j.Status });
    }
}

public class AsyncIORowErrorConfiguration : IEntityTypeConfiguration<AsyncIORowError>
{
    public void Configure(EntityTypeBuilder<AsyncIORowError> builder)
    {
        builder.Property(r => r.Field).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(r => r.JobId);
    }
}
