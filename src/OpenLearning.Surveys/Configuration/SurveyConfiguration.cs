using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Surveys.Models;

namespace OpenLearning.Surveys.Configuration;

public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.CreatedBy).HasMaxLength(450).IsRequired();
        builder.Property(s => s.TokenSalt).IsRequired();
        builder.HasIndex(s => new { s.Scope, s.CourseId });
        builder.HasMany(s => s.Questions)
            .WithOne(q => q.Survey)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
    {
        builder.Property(q => q.Prompt).HasMaxLength(500).IsRequired();
        builder.HasIndex(q => new { q.SurveyId, q.SortOrder });
        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyQuestionOptionConfiguration : IEntityTypeConfiguration<SurveyQuestionOption>
{
    public void Configure(EntityTypeBuilder<SurveyQuestionOption> builder)
    {
        builder.Property(o => o.Text).HasMaxLength(200).IsRequired();
        builder.HasIndex(o => new { o.QuestionId, o.SortOrder });
    }
}

public class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.Property(r => r.RespondentUserId).HasMaxLength(450);
        builder.Property(r => r.RespondentToken).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => new { r.SurveyId, r.RespondentToken }).IsUnique();
        builder.HasMany(r => r.Answers)
            .WithOne(a => a.Response)
            .HasForeignKey(a => a.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurveyAnswerConfiguration : IEntityTypeConfiguration<SurveyAnswer>
{
    public void Configure(EntityTypeBuilder<SurveyAnswer> builder)
    {
        builder.Property(a => a.TextValue).HasMaxLength(4000);
        builder.HasIndex(a => new { a.ResponseId, a.QuestionId }).IsUnique();
        builder.HasIndex(a => a.QuestionId);
    }
}
