using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.PracticalTraining.Models;

namespace OpenLearning.PracticalTraining.Configuration;

public sealed class PracticalProgramConfiguration : IEntityTypeConfiguration<PracticalProgram>
{
    public void Configure(EntityTypeBuilder<PracticalProgram> builder)
    {
        builder.ToTable("PracticalPrograms");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MinimumHours).HasPrecision(8, 2);
        builder.HasIndex(x => new { x.Title, x.Version }).IsUnique();
        builder.HasMany(x => x.Competencies).WithOne(x => x.Program).HasForeignKey(x => x.PracticalProgramId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProgramCompetencyConfiguration : IEntityTypeConfiguration<ProgramCompetency>
{
    public void Configure(EntityTypeBuilder<ProgramCompetency> builder)
    {
        builder.ToTable("PracticalProgramCompetencies");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class HostOrganizationConfiguration : IEntityTypeConfiguration<HostOrganization>
{
    public void Configure(EntityTypeBuilder<HostOrganization> builder)
    {
        builder.ToTable("PracticalHosts");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(320);
    }
}

public sealed class PlacementConfiguration : IEntityTypeConfiguration<Placement>
{
    public void Configure(EntityTypeBuilder<Placement> builder)
    {
        builder.ToTable("PracticalPlacements");
        builder.Property(x => x.LearnerId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.CoordinatorId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.SupervisorName).HasMaxLength(200);
        builder.Property(x => x.SupervisorEmail).HasMaxLength(320);
        builder.HasIndex(x => x.LearnerId);
        builder.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.PracticalProgramId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Host).WithMany().HasForeignKey(x => x.HostOrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Competencies).WithOne(x => x.Placement).HasForeignKey(x => x.PlacementId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlacementCompetencyConfiguration : IEntityTypeConfiguration<PlacementCompetency>
{
    public void Configure(EntityTypeBuilder<PlacementCompetency> builder)
    {
        builder.ToTable("PracticalPlacementCompetencies");
        builder.Property(x => x.Evaluation).HasMaxLength(2000);
        builder.HasIndex(x => new { x.PlacementId, x.ProgramCompetencyId }).IsUnique();
    }
}

public sealed class SupervisorInvitationConfiguration : IEntityTypeConfiguration<SupervisorInvitation>
{
    public void Configure(EntityTypeBuilder<SupervisorInvitation> builder)
    {
        builder.ToTable("PracticalSupervisorInvitations");
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
    }
}

public sealed class PracticalHourLogConfiguration : IEntityTypeConfiguration<PracticalHourLog>
{
    public void Configure(EntityTypeBuilder<PracticalHourLog> builder)
    {
        builder.ToTable("PracticalHourLogs");
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.ReviewNote).HasMaxLength(1000);
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasIndex(x => new { x.PlacementId, x.StartedAt, x.EndedAt });
        builder.HasOne(x => x.AmendsLog).WithMany().HasForeignKey(x => x.AmendsLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PracticalEvidenceConfiguration : IEntityTypeConfiguration<PracticalEvidence>
{
    public void Configure(EntityTypeBuilder<PracticalEvidence> builder)
    {
        builder.ToTable("PracticalEvidence");
        builder.Property(x => x.LearnerId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
    }
}

public sealed class PracticalEvaluationConfiguration : IEntityTypeConfiguration<PracticalEvaluation>
{
    public void Configure(EntityTypeBuilder<PracticalEvaluation> builder)
    {
        builder.ToTable("PracticalEvaluations");
        builder.Property(x => x.EvaluatorKind).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => new { x.PlacementId, x.EvaluatorKind }).IsUnique();
    }
}

public sealed class PlacementIncidentConfiguration : IEntityTypeConfiguration<PlacementIncident>
{
    public void Configure(EntityTypeBuilder<PlacementIncident> builder)
    {
        builder.ToTable("PracticalIncidents");
        builder.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Resolution).HasMaxLength(2000);
    }
}

public sealed class PracticalCompletionConfiguration : IEntityTypeConfiguration<PracticalCompletion>
{
    public void Configure(EntityTypeBuilder<PracticalCompletion> builder)
    {
        builder.ToTable("PracticalCompletions");
        builder.Property(x => x.ConfirmationKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ApprovedHours).HasPrecision(8, 2);
        builder.HasIndex(x => x.PlacementId).IsUnique();
        builder.HasIndex(x => x.ConfirmationKey).IsUnique();
    }
}
