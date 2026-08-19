using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Exams.Models;

namespace OpenLearning.Exams.Configuration;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => new { e.AuthorId, e.CourseId });
        builder.HasOne(e => e.Course)
               .WithMany()
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Author)
               .WithMany()
               .HasForeignKey(e => e.AuthorId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Questions)
               .WithOne()
               .HasForeignKey(q => q.ExamId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.HasIndex(a => new { a.ExamId, a.StudentId, a.Status });
        builder.HasIndex(a => new { a.StudentId, a.ExamId, a.StartedAt });
        builder.HasOne(a => a.Exam)
               .WithMany(e => e.Attempts)
               .HasForeignKey(a => a.ExamId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Student)
               .WithMany()
               .HasForeignKey(a => a.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamAttemptAnswerConfiguration : IEntityTypeConfiguration<ExamAttemptAnswer>
{
    public void Configure(EntityTypeBuilder<ExamAttemptAnswer> builder)
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
