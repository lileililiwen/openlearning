using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Configuration;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasIndex(a => new { a.QuizId, a.CompletedAt });
        builder.HasOne(a => a.Quiz)
               .WithMany()
               .HasForeignKey(a => a.QuizId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Student)
               .WithMany()
               .HasForeignKey(a => a.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
