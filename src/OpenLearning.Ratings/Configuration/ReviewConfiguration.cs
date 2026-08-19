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

public class ReviewCommentConfiguration : IEntityTypeConfiguration<ReviewComment>
{
    public void Configure(EntityTypeBuilder<ReviewComment> builder)
    {
        builder.Property(c => c.Body).HasMaxLength(2000).IsRequired();
        builder.HasIndex(c => new { c.ReviewId, c.CreatedAt });
        builder.HasIndex(c => new { c.ReviewId, c.AuthorId, c.Body }).IsUnique();
        builder.HasOne(c => c.Review)
               .WithMany(r => r.Comments)
               .HasForeignKey(c => c.ReviewId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Author)
               .WithMany()
               .HasForeignKey(c => c.AuthorId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
