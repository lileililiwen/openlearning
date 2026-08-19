using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Configuration;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.VideoUrl).HasMaxLength(1000);
        builder.Property(l => l.VideoPosterUrl).HasMaxLength(1000);
        builder.Property(l => l.SubtitleUrl).HasMaxLength(1000);
        builder.HasIndex(l => new { l.ModuleId, l.OrderIndex });
        builder.HasOne(l => l.Module)
               .WithMany(m => m.Lessons)
               .HasForeignKey(l => l.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
