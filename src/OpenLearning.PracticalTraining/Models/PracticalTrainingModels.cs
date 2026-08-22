namespace OpenLearning.PracticalTraining.Models;

public enum PlacementStatus { Draft, Active, Suspended, Completed, Cancelled }
public enum PracticalLogStatus { Submitted, Approved, Rejected, Superseded }
public enum IncidentSeverity { Informational, Blocking }

public sealed class PracticalProgram
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public decimal MinimumHours { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<ProgramCompetency> Competencies { get; set; } = new List<ProgramCompetency>();
}

public sealed class ProgramCompetency
{
    public int Id { get; set; }
    public int PracticalProgramId { get; set; }
    public PracticalProgram? Program { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
}

public sealed class HostOrganization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}

public sealed class Placement
{
    public int Id { get; set; }
    public int PracticalProgramId { get; set; }
    public PracticalProgram? Program { get; set; }
    public int HostOrganizationId { get; set; }
    public HostOrganization? Host { get; set; }
    public string LearnerId { get; set; } = string.Empty;
    public string CoordinatorId { get; set; } = string.Empty;
    public string SupervisorName { get; set; } = string.Empty;
    public string SupervisorEmail { get; set; } = string.Empty;
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public PlacementStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<PlacementCompetency> Competencies { get; set; } = new List<PlacementCompetency>();
}

public sealed class PlacementCompetency
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public Placement? Placement { get; set; }
    public int ProgramCompetencyId { get; set; }
    public ProgramCompetency? ProgramCompetency { get; set; }
    public bool IsAchieved { get; set; }
    public string Evaluation { get; set; } = string.Empty;
    public DateTime? EvaluatedAt { get; set; }
}

public sealed class SupervisorInvitation
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public Placement? Placement { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PracticalHourLog
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public Placement? Placement { get; set; }
    public int? AmendsLogId { get; set; }
    public PracticalHourLog? AmendsLog { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public PracticalLogStatus Status { get; set; } = PracticalLogStatus.Submitted;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

public sealed class PracticalEvidence
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public int StoredFileId { get; set; }
    public string LearnerId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PracticalEvaluation
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public string EvaluatorKind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlacementIncident
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public IncidentSeverity Severity { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}

public sealed class PracticalCompletion
{
    public int Id { get; set; }
    public int PlacementId { get; set; }
    public string ConfirmationKey { get; set; } = string.Empty;
    public decimal ApprovedHours { get; set; }
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
    public string ConfirmedBy { get; set; } = string.Empty;
}
