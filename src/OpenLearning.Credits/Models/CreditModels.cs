using OpenLearning.Auth.Models;

namespace OpenLearning.Credits.Models;

public enum CreditCategory
{
    General = 0,
    Major = 1,
    Elective = 2,
    Lab = 3,
}

/// <summary>Append-only ledger entry for a credit award or revocation.</summary>
public class CreditAward
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    /// <summary>Credit amount (negative for revocations).</summary>
    public decimal Amount { get; set; }

    public CreditCategory Category { get; set; }

    /// <summary>Source type (e.g. "course-completion", "admin-adjustment").</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>Source id for idempotency (e.g. course-completion id).</summary>
    public string? SourceId { get; set; }

    /// <summary>Rule version this award was evaluated under.</summary>
    public int RuleVersion { get; set; }

    public string? Reason { get; set; }

    /// <summary>Actor who triggered or authorized this entry.</summary>
    public string ActorId { get; set; } = string.Empty;

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Published credit rule applied when a learner completes a course.</summary>
public class CourseCreditRule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public decimal Amount { get; set; }
    public CreditCategory Category { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Graduation program with versioned requirements.</summary>
public class GraduationProgram
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Version { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Minimum total credits required.</summary>
    public decimal MinTotalCredits { get; set; }

    /// <summary>JSON: dictionary of CreditCategory -> minimum credits.</summary>
    public string CategoryMinimums { get; set; } = "{}";

    /// <summary>JSON: list of required course ids.</summary>
    public string RequiredCourseIds { get; set; } = "[]";

    /// <summary>Ignore awards older than this many days; null means credits never expire.</summary>
    public int? CreditExpiryDays { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Links a learner to a program version.</summary>
public class LearnerProgram
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int ProgramId { get; set; }

    public GraduationProgram? Program { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public enum GraduationDecisionType
{
    None = 0,
    Eligible = 1,
    Graduated = 2,
    Denied = 3,
}

/// <summary>Records an explicit graduation decision for a learner.</summary>
public class GraduationDecision
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int ProgramId { get; set; }

    public GraduationProgram? Program { get; set; }

    public GraduationDecisionType Decision { get; set; }

    public string? Notes { get; set; }

    /// <summary>Actor who recorded this decision.</summary>
    public string ActorId { get; set; } = string.Empty;

    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
