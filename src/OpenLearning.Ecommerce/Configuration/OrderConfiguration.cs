using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Ecommerce.Models;

namespace OpenLearning.Ecommerce.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.Amount).HasPrecision(10, 2);
        builder.Property(o => o.PaymentReference).HasMaxLength(100);
        builder.Property(o => o.DiscountAmount).HasPrecision(10, 2);
        builder.Property(o => o.PaidWithBalance).HasPrecision(10, 2);
        builder.HasIndex(o => new { o.CourseId, o.Status });
        builder.HasIndex(o => new { o.StudentId, o.CourseId });
        builder.HasIndex(o => o.CartCheckoutId);
        builder.HasOne(o => o.Course)
               .WithMany()
               .HasForeignKey(o => o.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Student)
               .WithMany()
               .HasForeignKey(o => o.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Coupon)
               .WithMany()
               .HasForeignKey(o => o.CouponId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
