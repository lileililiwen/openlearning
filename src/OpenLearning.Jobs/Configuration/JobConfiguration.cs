using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Jobs.Models;

namespace OpenLearning.Jobs.Configuration;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.Property(j => j.Key).HasMaxLength(100).IsRequired();
        builder.Property(j => j.Cron).HasMaxLength(100).IsRequired();
        builder.Property(j => j.LockToken).HasMaxLength(64);
        builder.HasIndex(j => j.Key).IsUnique();
        builder.HasIndex(j => j.NextRunAt);
    }
}

public class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.Property(r => r.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(r => r.LockToken).HasMaxLength(64);
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(r => new { r.JobId, r.StartedAt });
        builder.HasIndex(r => new { r.IdempotencyKey, r.Status });
        builder.HasOne(r => r.Job).WithMany().HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}
