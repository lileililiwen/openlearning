using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Distribution.Models;

namespace OpenLearning.Distribution.Configuration;

public class DistributorProfileConfiguration : IEntityTypeConfiguration<DistributorProfile>
{
    public void Configure(EntityTypeBuilder<DistributorProfile> builder)
    {
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AffiliateLinkConfiguration : IEntityTypeConfiguration<AffiliateLink>
{
    public void Configure(EntityTypeBuilder<AffiliateLink> builder)
    {
        builder.Property(l => l.Slug).HasMaxLength(40).IsRequired();
        builder.HasIndex(l => l.Slug).IsUnique();
        builder.HasIndex(l => new { l.DistributorUserId, l.CourseId });
        builder.HasOne(l => l.Course).WithMany().HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AffiliateClickConfiguration : IEntityTypeConfiguration<AffiliateClick>
{
    public void Configure(EntityTypeBuilder<AffiliateClick> builder)
    {
        builder.Property(c => c.AnonymousId).HasMaxLength(64).IsRequired();
        builder.Property(c => c.HashedIp).HasMaxLength(64);
        builder.Property(c => c.UserAgent).HasMaxLength(512);
        builder.HasIndex(c => new { c.AffiliateLinkId, c.ClickedAt });
        builder.HasOne(c => c.AffiliateLink).WithMany().HasForeignKey(c => c.AffiliateLinkId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttributionConfiguration : IEntityTypeConfiguration<Attribution>
{
    public void Configure(EntityTypeBuilder<Attribution> builder)
    {
        builder.HasIndex(a => a.OrderId).IsUnique();
        builder.HasIndex(a => new { a.DistributorUserId, a.CreatedAt });
    }
}

public class CommissionEntryConfiguration : IEntityTypeConfiguration<CommissionEntry>
{
    public void Configure(EntityTypeBuilder<CommissionEntry> builder)
    {
        builder.Property(c => c.Amount).HasPrecision(10, 2);
        builder.HasIndex(c => new { c.DistributorUserId, c.Status });
        builder.HasIndex(c => new { c.OrderId, c.Status });
    }
}

public class PayoutRequestConfiguration : IEntityTypeConfiguration<PayoutRequest>
{
    public void Configure(EntityTypeBuilder<PayoutRequest> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.HasIndex(p => new { p.DistributorUserId, p.Status });
        builder.HasIndex(p => p.Status);
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.DistributorUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DistributorSettlementStatementConfiguration : IEntityTypeConfiguration<DistributorSettlementStatement>
{
    public void Configure(EntityTypeBuilder<DistributorSettlementStatement> builder)
    {
        builder.Property(s => s.TotalAmount).HasPrecision(10, 2);
        builder.HasIndex(s => new { s.DistributorUserId, s.PeriodStart }).IsUnique();
    }
}
