namespace OpenLearning.Competency.Models;

public enum EvidenceStatus
{
    /// <summary>Created automatically from a trusted completion; approved by construction.</summary>
    Auto = 0,

    /// <summary>Manual submission awaiting review.</summary>
    Pending = 1,

    /// <summary>Manual submission approved by a reviewer.</summary>
    Approved = 2,

    /// <summary>Manual submission rejected by a reviewer.</summary>
    Rejected = 3,
}

/// <summary>A versioned competency framework (e.g. "Software Engineering Core Skills").</summary>
public sealed class CompetencyFramework
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Bumped whenever the structure is edited; earned evidence pins the version it satisfied.</summary>
    public int Version { get; set; } = 1;

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FrameworkScaleLevel> ScaleLevels { get; set; } = new List<FrameworkScaleLevel>();

    public ICollection<CompetencyNode> Competencies { get; set; } = new List<CompetencyNode>();
}

/// <summary>One labeled step of the framework's achievement scale (e.g. 1..5, "Novice".."Expert").</summary>
public sealed class FrameworkScaleLevel
{
    public int Id { get; set; }

    public int FrameworkId { get; set; }

    public CompetencyFramework? Framework { get; set; }

    public int SortOrder { get; set; }

    public string Label { get; set; } = string.Empty;
}

/// <summary>A competency inside a framework; optionally nested under a parent competency.</summary>
public sealed class CompetencyNode
{
    public int Id { get; set; }

    public int FrameworkId { get; set; }

    public CompetencyFramework? Framework { get; set; }

    public int? ParentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Links a course or an assignment to a competency so completions produce evidence.</summary>
public sealed class ActivityMapping
{
    public int Id { get; set; }

    public int CompetencyId { get; set; }

    public CompetencyNode? Competency { get; set; }

    /// <summary>Set when the mapping targets whole-course completion (100% of lessons).</summary>
    public int? CourseId { get; set; }

    /// <summary>Set when the mapping targets a graded assignment submission.</summary>
    public int? AssignmentId { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Evidence that a learner attained a competency; automatic rows are pre-approved.</summary>
public sealed class CompetencyEvidence
{
    public int Id { get; set; }

    public int CompetencyId { get; set; }

    public CompetencyNode? Competency { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Idempotency key, e.g. "course:3:user:abc" or "assignment:9:user:abc" or "manual:{guid}".</summary>
    public string SourceKey { get; set; } = string.Empty;

    public EvidenceStatus Status { get; set; }

    /// <summary>Scale level at creation/approval time (1-based sort order).</summary>
    public int? LevelSortOrder { get; set; }

    /// <summary>Framework version pinned when the evidence was created.</summary>
    public int FrameworkVersion { get; set; }

    /// <summary>Competency title snapshot pinned when the evidence was created.</summary>
    public string CompetencyTitleSnapshot { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? ReviewerId { get; set; }

    public string? ReviewReason { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
