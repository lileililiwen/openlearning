namespace OpenLearning.Gradebook.Models;

public enum GradebookItemKind
{
    Assignment = 0,
    Quiz = 1,
    Exam = 2,
}

/// <summary>Per-course gradebook configuration and publication state.</summary>
public sealed class GradebookConfig
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? PublishedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GradebookItem> Items { get; set; } = new List<GradebookItem>();
}

/// <summary>One weighted graded activity in the course gradebook.</summary>
public sealed class GradebookItem
{
    public int Id { get; set; }

    public int ConfigId { get; set; }

    public GradebookConfig? Config { get; set; }

    public GradebookItemKind Kind { get; set; }

    /// <summary>The assignment, quiz, or exam id, depending on Kind.</summary>
    public int SourceId { get; set; }

    /// <summary>Weight in percent; active items must total exactly 100 before publication.</summary>
    public int Weight { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>An instructor-applied score override or excusal for one student on one item.</summary>
public sealed class GradebookAdjustment
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public GradebookItem? Item { get; set; }

    public string StudentId { get; set; } = string.Empty;

    /// <summary>When true the item is excluded from numerator and denominator for this student.</summary>
    public bool IsExcusal { get; set; }

    /// <summary>Gradebook-local replacement score on the 0-100 scale (overrides only).</summary>
    public int? OverrideScore { get; set; }

    public string? Reason { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Audit record of every student's aggregate at publication time.</summary>
public sealed class GradebookSnapshot
{
    public int Id { get; set; }

    public int ConfigId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public int? Aggregate { get; set; }

    /// <summary>Human-readable record of the inputs used, e.g. "assignment:5=80; quiz:2=72; excused:exam:1".</summary>
    public string BasisJson { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
