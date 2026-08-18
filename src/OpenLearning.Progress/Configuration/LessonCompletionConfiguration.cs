using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Progress.Models;

namespace OpenLearning.Progress.Configuration;

public class LessonCompletionConfiguration : IEntityTypeConfiguration<LessonCompletion>
{
    public void Configure(EntityTypeBuilder<LessonCompletion> builder)
    {
        builder.HasIndex(lc => new { lc.EnrollmentId, lc.LessonId }).IsUnique();
        builder.HasOne(lc => lc.Enrollment)
               .WithMany()
               .HasForeignKey(lc => lc.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(lc => lc.Lesson)
               .WithMany()
               .HasForeignKey(lc => lc.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
