using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.PeerAssessment.Models;

namespace OpenLearning.PeerAssessment.Configuration;

public class PeerReviewConfigConfiguration : IEntityTypeConfiguration<PeerReviewConfig>
{
    public void Configure(EntityTypeBuilder<PeerReviewConfig> builder)
    {
        builder.Property(c => c.ReleasedBy).HasMaxLength(450);
        builder.HasIndex(c => c.AssignmentId).IsUnique();
        builder.HasIndex(c => c.CourseId);
        builder.HasMany(c => c.RubricQuestions)
            .WithOne(q => q.Config)
            .HasForeignKey(q => q.ConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PeerReviewRubricQuestionConfiguration : IEntityTypeConfiguration<PeerReviewRubricQuestion>
{
    public void Configure(EntityTypeBuilder<PeerReviewRubricQuestion> builder)
    {
        builder.Property(q => q.Prompt).HasMaxLength(500).IsRequired();
        builder.HasIndex(q => new { q.ConfigId, q.SortOrder });
    }
}

public class PeerAllocationRunConfiguration : IEntityTypeConfiguration<PeerAllocationRun>
{
    public void Configure(EntityTypeBuilder<PeerAllocationRun> builder)
    {
        builder.Property(r => r.CreatedBy).HasMaxLength(450).IsRequired();
        builder.HasIndex(r => new { r.ConfigId, r.RunNumber }).IsUnique();
    }
}

public class PeerAllocationPairConfiguration : IEntityTypeConfiguration<PeerAllocationPair>
{
    public void Configure(EntityTypeBuilder<PeerAllocationPair> builder)
    {
        builder.Property(p => p.ReviewerId).HasMaxLength(450).IsRequired();
        builder.HasIndex(p => new { p.RunId, p.ReviewerId, p.RevieweeSubmissionId }).IsUnique();
        builder.HasIndex(p => new { p.ConfigId, p.ReviewerId, p.IsActive });
        builder.HasIndex(p => p.RevieweeSubmissionId);
    }
}

public class PeerAssessmentConfiguration : IEntityTypeConfiguration<PeerReviewAssessment>
{
    public void Configure(EntityTypeBuilder<PeerReviewAssessment> builder)
    {
        builder.Property(a => a.AssessorId).HasMaxLength(450).IsRequired();
        builder.HasIndex(a => new { a.ConfigId, a.AssessorId, a.RevieweeSubmissionId }).IsUnique();
        builder.HasIndex(a => a.RevieweeSubmissionId);
        builder.HasMany(a => a.Answers)
            .WithOne(x => x.Assessment)
            .HasForeignKey(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PeerAssessmentAnswerConfiguration : IEntityTypeConfiguration<PeerAssessmentAnswer>
{
    public void Configure(EntityTypeBuilder<PeerAssessmentAnswer> builder)
    {
        builder.Property(a => a.PromptSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Comment).HasMaxLength(2000);
        builder.HasIndex(a => new { a.AssessmentId, a.QuestionId }).IsUnique();
    }
}

public class PeerReviewResultConfiguration : IEntityTypeConfiguration<PeerReviewResult>
{
    public void Configure(EntityTypeBuilder<PeerReviewResult> builder)
    {
        builder.Property(r => r.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.Basis).HasMaxLength(50).IsRequired();
        builder.Property(r => r.OverrideBy).HasMaxLength(450);
        builder.HasIndex(r => new { r.ConfigId, r.StudentId }).IsUnique();
    }
}
