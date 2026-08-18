using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Scorm.Models;

namespace OpenLearning.Scorm.Configuration;

public class ScormPackageConfiguration : IEntityTypeConfiguration<ScormPackage>
{
    public void Configure(EntityTypeBuilder<ScormPackage> builder)
    {
        builder.Property(p => p.Title).HasMaxLength(500);
        builder.Property(p => p.ScormVersion).HasMaxLength(20);
        builder.Property(p => p.EntryPoint).HasMaxLength(500);
        builder.Property(p => p.PackagePath).HasMaxLength(500);
        builder.HasIndex(p => p.LessonId).IsUnique();
        builder.HasOne(p => p.Lesson)
               .WithMany()
               .HasForeignKey(p => p.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
