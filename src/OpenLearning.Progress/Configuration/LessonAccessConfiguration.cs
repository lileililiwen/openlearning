using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Progress.Models;

namespace OpenLearning.Progress.Configuration;

public class LessonAccessConfiguration : IEntityTypeConfiguration<LessonAccess>
{
    public void Configure(EntityTypeBuilder<LessonAccess> builder)
    {
        builder.HasIndex(la => new { la.EnrollmentId, la.LessonId }).IsUnique();
        builder.HasOne(la => la.Enrollment)
               .WithMany()
               .HasForeignKey(la => la.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(la => la.Lesson)
               .WithMany()
               .HasForeignKey(la => la.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
