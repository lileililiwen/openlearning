using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Configuration;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.Property(q => q.Text).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.BankTopic).HasMaxLength(200);
        builder.HasIndex(q => new { q.QuizId, q.OrderIndex });
        builder.HasIndex(q => new { q.ExamId, q.OrderIndex });
        builder.HasIndex(q => new { q.IsBank, q.ArchivedAt });
        builder.HasOne(q => q.Quiz)
               .WithMany(q => q.Questions)
               .HasForeignKey(q => q.QuizId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
