using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Ecommerce.Models;

namespace OpenLearning.Ecommerce.Configuration;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasIndex(c => new { c.StudentId, c.CourseId }).IsUnique();
        builder.HasOne(c => c.Course)
               .WithMany()
               .HasForeignKey(c => c.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.DiscountAmount).HasPrecision(10, 2);
    }
}

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.HasIndex(r => new { r.CouponId, r.UserId }).IsUnique();
        builder.HasOne(r => r.Coupon)
               .WithMany()
               .HasForeignKey(r => r.CouponId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BalanceLedgerConfiguration : IEntityTypeConfiguration<BalanceLedger>
{
    public void Configure(EntityTypeBuilder<BalanceLedger> builder)
    {
        builder.Property(l => l.Amount).HasPrecision(10, 2);
        builder.Property(l => l.Reason).HasMaxLength(200).IsRequired();
        builder.HasIndex(l => new { l.UserId, l.CreatedAt });
    }
}

public class PointsLedgerConfiguration : IEntityTypeConfiguration<PointsLedger>
{
    public void Configure(EntityTypeBuilder<PointsLedger> builder)
    {
        builder.Property(l => l.Reason).HasMaxLength(200).IsRequired();
        builder.HasIndex(l => new { l.UserId, l.CreatedAt });
    }
}

public class InvoiceRequestConfiguration : IEntityTypeConfiguration<InvoiceRequest>
{
    public void Configure(EntityTypeBuilder<InvoiceRequest> builder)
    {
        builder.HasOne(i => i.Order)
               .WithMany()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(i => i.OrderId).IsUnique();
    }
}
