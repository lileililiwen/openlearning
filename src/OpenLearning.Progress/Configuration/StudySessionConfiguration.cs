using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Progress.Models;

namespace OpenLearning.Progress.Configuration;

public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.CourseId, s.LessonId });
        builder.HasIndex(s => new { s.UserId, s.CourseId, s.LessonId });
        builder.HasIndex(s => s.EnrollmentId);
        builder.HasOne(s => s.Lesson)
               .WithMany()
               .HasForeignKey(s => s.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Enrollment)
               .WithMany()
               .HasForeignKey(s => s.EnrollmentId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
