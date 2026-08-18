using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Ratings.Models;

namespace OpenLearning.Ratings.Configuration;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.HasIndex(r => new { r.CourseId, r.UserId }).IsUnique();
        builder.HasOne(r => r.Course)
               .WithMany()
               .HasForeignKey(r => r.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.User)
               .WithMany()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
