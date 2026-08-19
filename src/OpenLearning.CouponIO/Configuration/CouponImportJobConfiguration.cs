using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.CouponIO.Models;

namespace OpenLearning.CouponIO.Configuration;

public class CouponImportJobConfiguration : IEntityTypeConfiguration<CouponImportJob>
{
    public void Configure(EntityTypeBuilder<CouponImportJob> builder)
    {
        builder.Property(j => j.UserId).HasMaxLength(450).IsRequired();
        builder.Property(j => j.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(j => j.ErrorFileKey).HasMaxLength(500);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => j.AsyncIOJobId).IsUnique();
    }
}

public class CouponImportRowErrorConfiguration : IEntityTypeConfiguration<CouponImportRowError>
{
    public void Configure(EntityTypeBuilder<CouponImportRowError> builder)
    {
        builder.Property(e => e.Field).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(e => e.JobId);
    }
}
