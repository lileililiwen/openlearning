using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.AI.Models;

namespace OpenLearning.AI.Configuration;

public sealed class AiPolicyConfiguration : IEntityTypeConfiguration<AiPolicy>
{
    public void Configure(EntityTypeBuilder<AiPolicy> builder)
    {
        builder.ToTable("AiPolicies");
        builder.HasIndex(x => x.CourseId).IsUnique();
        builder.Property(x => x.Provider).HasMaxLength(80);
        builder.Property(x => x.Model).HasMaxLength(120);
        builder.Property(x => x.SecretReference).HasMaxLength(200);
        builder.Property(x => x.ExternalProcessingDisclosure).HasMaxLength(2000);
        builder.Property(x => x.CostPerThousandTokens).HasPrecision(18, 6);
    }
}

public sealed class AiApprovedSourceConfiguration : IEntityTypeConfiguration<AiApprovedSource>
{
    public void Configure(EntityTypeBuilder<AiApprovedSource> builder)
    {
        builder.ToTable("AiApprovedSources");
        builder.HasIndex(x => new { x.CourseId, x.Anchor }).IsUnique();
        builder.Property(x => x.Title).HasMaxLength(300);
        builder.Property(x => x.Anchor).HasMaxLength(500);
        builder.Property(x => x.ApprovedById).HasMaxLength(450);
    }
}

public sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("AiConversations");
        builder.HasIndex(x => new { x.UserId, x.CourseId });
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.HasMany(x => x.Messages).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("AiMessages");
        builder.HasMany(x => x.Citations).WithOne(x => x.Message).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AiCitationConfiguration : IEntityTypeConfiguration<AiCitation>
{
    public void Configure(EntityTypeBuilder<AiCitation> builder)
    {
        builder.ToTable("AiCitations");
        builder.Property(x => x.Title).HasMaxLength(300);
        builder.Property(x => x.Anchor).HasMaxLength(500);
    }
}

public sealed class AiFeedbackDraftConfiguration : IEntityTypeConfiguration<AiFeedbackDraft>
{
    public void Configure(EntityTypeBuilder<AiFeedbackDraft> builder)
    {
        builder.ToTable("AiFeedbackDrafts");
        builder.HasIndex(x => x.AssignmentSubmissionId);
        builder.Property(x => x.RequestedById).HasMaxLength(450);
        builder.Property(x => x.ConfirmedById).HasMaxLength(450);
    }
}

public sealed class AiUsageAuditConfiguration : IEntityTypeConfiguration<AiUsageAudit>
{
    public void Configure(EntityTypeBuilder<AiUsageAudit> builder)
    {
        builder.ToTable("AiUsageAudits");
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.Property(x => x.Cost).HasPrecision(18, 6);
        builder.Property(x => x.UserId).HasMaxLength(450);
    }
}

public sealed class AiOutputReportConfiguration : IEntityTypeConfiguration<AiOutputReport>
{
    public void Configure(EntityTypeBuilder<AiOutputReport> builder)
    {
        builder.ToTable("AiOutputReports");
        builder.HasIndex(x => new { x.MessageId, x.ReportedById }).IsUnique();
        builder.Property(x => x.ReportedById).HasMaxLength(450);
        builder.Property(x => x.Reason).HasMaxLength(1000);
    }
}
