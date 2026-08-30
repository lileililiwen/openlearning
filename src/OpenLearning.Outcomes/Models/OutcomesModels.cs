namespace OpenLearning.Outcomes.Models;

/// <summary>Mastery state of a learner against a single course outcome.</summary>
public enum MasteryState
{
    NotStarted = 0,
    Developing = 1,
    Mastered = 2,
    NeedsReview = 3,
}

/// <summary>
/// A course-level outcome declared by the owning instructor. Mastery is computed
/// against the mapped activities using <see cref="CourseOutcome.MasteryThreshold"/>.
/// </summary>
public sealed class CourseOutcome
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    /// <summary>Owning instructor; mirrors CourseManagement Course.InstructorId.</summary>
    public string OwnerId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Weighted-score fraction (0..1) required to consider the outcome mastered.</summary>
    public double MasteryThreshold { get; set; } = 0.7;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Maps a course outcome to one assessable activity with a relative weight. The
/// activity is referenced by id/type only; the Outcomes module never navigates to
/// the source module's entities (modular-monolith rule).
/// </summary>
public sealed class OutcomeActivityMapping
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int OutcomeId { get; set; }

    /// <summary>Id of the activity in its source module (e.g. Quiz.Id, Exam.Id).</summary>
    public int ActivityId { get; set; }

    /// <summary>Discriminator for the source module: "Quiz", "Assignment", or "Exam".</summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>Relative weight of this activity within the outcome (must be greater than 0).</summary>
    public double Weight { get; set; } = 1.0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The current (single) mastery computation for a learner against an outcome. The
/// calculation is idempotent: recalculating with the same inputs upserts this row
/// rather than creating conflicting current results.
/// </summary>
public sealed class MasteryResult
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int OutcomeId { get; set; }

    public string LearnerId { get; set; } = string.Empty;

    /// <summary>Version of the deterministic calculation rule that produced this result.</summary>
    public int CalculationVersion { get; set; } = 1;

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Weighted score fraction (0..1) across the mapped activities.</summary>
    public double Score { get; set; }

    public MasteryState State { get; set; } = MasteryState.NotStarted;

    /// <summary>Recorded inputs and calculation metadata, retained for explanation.</summary>
    public string SourceJson { get; set; } = "{}";
}
