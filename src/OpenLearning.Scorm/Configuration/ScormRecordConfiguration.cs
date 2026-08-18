using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Scorm.Models;

namespace OpenLearning.Scorm.Configuration;

public class ScormRecordConfiguration : IEntityTypeConfiguration<ScormRecord>
{
    public void Configure(EntityTypeBuilder<ScormRecord> builder)
    {
        builder.Property(r => r.LessonStatus).HasMaxLength(50);
        builder.Property(r => r.LessonLocation).HasMaxLength(2000);
        builder.Property(r => r.SuspendData).HasMaxLength(20000);
        builder.Property(r => r.ScoreRaw).HasMaxLength(50);
        builder.Property(r => r.SessionTime).HasMaxLength(100);
        builder.HasIndex(r => new { r.EnrollmentId, r.ScormPackageId }).IsUnique();
        builder.HasOne(r => r.Enrollment)
               .WithMany()
               .HasForeignKey(r => r.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.ScormPackage)
               .WithMany()
               .HasForeignKey(r => r.ScormPackageId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
