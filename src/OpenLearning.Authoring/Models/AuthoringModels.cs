namespace OpenLearning.Authoring.Models;

/// <summary>Lifecycle state of a single course revision.</summary>
public enum RevisionState
{
    Draft = 0,
    Published = 1,
}

/// <summary>
/// One snapshot of a course's authoring content. A draft is mutable; a published
/// revision is immutable and is the only revision learners may read.
/// </summary>
public sealed class CourseRevision
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    /// <summary>Monotonically increasing per course; never reused.</summary>
    public int RevisionNumber { get; set; }

    public RevisionState State { get; set; } = RevisionState.Draft;

    /// <summary>Owning instructor; mirrors CourseManagement Course.InstructorId at draft creation.</summary>
    public string OwnerId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string LearningOutcomes { get; set; } = string.Empty;

    public string Prerequisites { get; set; } = string.Empty;

    /// <summary>Ordered module/lesson content as JSON (see <see cref="ContentSnapshot"/>).</summary>
    public string ContentSnapshotJson { get; set; } = "{}";

    public string? PublishedBy { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Per-course pointer to the single mutable draft and single active published revision.</summary>
public sealed class CourseRevisionPointer
{
    public int CourseId { get; set; }

    public int? DraftRevisionId { get; set; }

    public int? ActiveRevisionId { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Optimistic-concurrency token bumped on every mutation so concurrent publishes conflict.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>A recorded validation pass over a draft.</summary>
public sealed class RevisionValidationResult
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int RevisionId { get; set; }

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public string CheckedBy { get; set; } = string.Empty;

    public bool IsValid { get; set; }

    public string ErrorsJson { get; set; } = "[]";
}

/// <summary>Auditable lifecycle action (publish, rollback, preview, etc.).</summary>
public sealed class RevisionAudit
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int? RevisionId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ActorId { get; set; } = string.Empty;

    public DateTime At { get; set; } = DateTime.UtcNow;

    public string? DetailJson { get; set; }
}

/// <summary>Ordered content snapshot stored in <see cref="CourseRevision.ContentSnapshotJson"/>.</summary>
internal sealed class ContentSnapshot
{
    public List<ContentModule> Modules { get; set; } = new();
}

internal sealed class ContentModule
{
    public string Title { get; set; } = string.Empty;

    public List<ContentLesson> Lessons { get; set; } = new();
}

internal sealed class ContentLesson
{
    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}
