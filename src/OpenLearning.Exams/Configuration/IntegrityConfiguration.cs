using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Exams.Models;

namespace OpenLearning.Exams.Configuration;

public class IntegrityPolicyConfiguration : IEntityTypeConfiguration<IntegrityPolicy>
{
    public void Configure(EntityTypeBuilder<IntegrityPolicy> builder)
    {
        builder.Property(p => p.RiskThreshold);
        builder.HasIndex(p => new { p.ExamId, p.IsActive });
        builder.HasOne(p => p.Exam)
               .WithMany()
               .HasForeignKey(p => p.ExamId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntegritySessionConfiguration : IEntityTypeConfiguration<IntegritySession>
{
    public void Configure(EntityTypeBuilder<IntegritySession> builder)
    {
        builder.Property(s => s.Nonce).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Signature).HasMaxLength(128).IsRequired();
        builder.HasIndex(s => s.AttemptId);
        builder.HasIndex(s => new { s.AttemptId, s.Status });
        builder.HasOne(s => s.Attempt)
               .WithMany()
               .HasForeignKey(s => s.AttemptId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntegrityEvidenceConfiguration : IEntityTypeConfiguration<IntegrityEvidence>
{
    public void Configure(EntityTypeBuilder<IntegrityEvidence> builder)
    {
        builder.Property(e => e.BatchId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Payload).HasMaxLength(500);
        builder.HasIndex(e => new { e.SessionId, e.BatchId });
        builder.HasIndex(e => e.AttemptId);
        builder.HasIndex(e => e.ReceivedAt);
        builder.HasOne(e => e.Session)
               .WithMany(s => s.Evidence)
               .HasForeignKey(e => e.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LearnerAccommodationConfiguration : IEntityTypeConfiguration<LearnerAccommodation>
{
    public void Configure(EntityTypeBuilder<LearnerAccommodation> builder)
    {
        builder.HasIndex(a => new { a.ExamId, a.StudentId });
        builder.HasOne(a => a.Exam)
               .WithMany()
               .HasForeignKey(a => a.ExamId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Student)
               .WithMany()
               .HasForeignKey(a => a.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Attempt)
               .WithMany()
               .HasForeignKey(a => a.AttemptId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class IntegrityIncidentConfiguration : IEntityTypeConfiguration<IntegrityIncident>
{
    public void Configure(EntityTypeBuilder<IntegrityIncident> builder)
    {
        builder.Property(i => i.ContributingRules).HasMaxLength(4000).IsRequired();
        builder.HasIndex(i => i.ExamId);
        builder.HasIndex(i => i.CourseId);
        builder.HasIndex(i => i.StudentId);
        builder.HasIndex(i => i.Status);
        builder.HasOne(i => i.Attempt)
               .WithMany()
               .HasForeignKey(i => i.AttemptId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntegrityDispositionConfiguration : IEntityTypeConfiguration<IntegrityDisposition>
{
    public void Configure(EntityTypeBuilder<IntegrityDisposition> builder)
    {
        builder.Property(d => d.Notes).HasMaxLength(2000);
        builder.HasIndex(d => d.IncidentId);
        builder.HasOne(d => d.Incident)
               .WithMany(i => i.Dispositions)
               .HasForeignKey(d => d.IncidentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.Reviewer)
               .WithMany()
               .HasForeignKey(d => d.ReviewerId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class IntegrityAppealConfiguration : IEntityTypeConfiguration<IntegrityAppeal>
{
    public void Configure(EntityTypeBuilder<IntegrityAppeal> builder)
    {
        builder.Property(a => a.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.ReviewerNotes).HasMaxLength(2000);
        builder.HasIndex(a => a.IncidentId);
        builder.HasOne(a => a.Incident)
               .WithMany(i => i.Appeals)
               .HasForeignKey(a => a.IncidentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Student)
               .WithMany()
               .HasForeignKey(a => a.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntegrityAccessLogConfiguration : IEntityTypeConfiguration<IntegrityAccessLog>
{
    public void Configure(EntityTypeBuilder<IntegrityAccessLog> builder)
    {
        builder.HasIndex(l => l.IncidentId);
        builder.HasIndex(l => l.ReviewerId);
        builder.HasIndex(l => l.AccessedAt);
    }
}
