using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Configuration;

public class QuizAttemptAnswerConfiguration : IEntityTypeConfiguration<QuizAttemptAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAttemptAnswer> builder)
    {
        builder.HasIndex(a => a.AttemptId);
        builder.Property(a => a.SelectedOptionIds).HasMaxLength(200);
        builder.Property(a => a.TextAnswer).HasMaxLength(2000);
        builder.Property(a => a.FileAnswerUrl).HasMaxLength(1000);
        builder.Property(a => a.GradingFeedback).HasMaxLength(2000);
        builder.HasOne(a => a.Attempt)
               .WithMany(a => a.Answers)
               .HasForeignKey(a => a.AttemptId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Question)
               .WithMany()
               .HasForeignKey(a => a.QuestionId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.AnswerOption)
               .WithMany()
               .HasForeignKey(a => a.AnswerOptionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
