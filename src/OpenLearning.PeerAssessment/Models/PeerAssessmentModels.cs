namespace OpenLearning.PeerAssessment.Models;

public enum PeerReviewStrategy
{
    InstructorOnly = 0,
    PeerAverage = 1,
    WeightedMix = 2,
}

public enum PeerReviewPhase
{
    Submission = 0,
    Review = 1,
    Closed = 2,
}

/// <summary>Peer review configuration for one assignment (one row per assignment).</summary>
public sealed class PeerReviewConfig
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public int CourseId { get; set; }

    /// <summary>How many peers review each submission (cohort size permitting).</summary>
    public int ReviewsPerStudent { get; set; }

    public bool IsAnonymous { get; set; }

    public PeerReviewStrategy Strategy { get; set; } = PeerReviewStrategy.InstructorOnly;

    /// <summary>Instructor weight in percent when Strategy is WeightedMix.</summary>
    public int InstructorWeightPercent { get; set; } = 60;

    public DateTime ReviewOpensAt { get; set; }

    public DateTime ReviewClosesAt { get; set; }

    /// <summary>When results were released to students (null until released).</summary>
    public DateTime? ResultsReleasedAt { get; set; }

    public string? ReleasedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PeerReviewRubricQuestion> RubricQuestions { get; set; } = new List<PeerReviewRubricQuestion>();
}

/// <summary>One rubric criterion. Immutable once the review phase opens.</summary>
public sealed class PeerReviewRubricQuestion
{
    public int Id { get; set; }

    public int ConfigId { get; set; }

    public PeerReviewConfig? Config { get; set; }

    public int SortOrder { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public int MaxPoints { get; set; }
}

/// <summary>An auditable, reproducible allocation run (run 1 is the automatic one).</summary>
public sealed class PeerAllocationRun
{
    public int Id { get; set; }

    public int ConfigId { get; set; }

    public int RunNumber { get; set; }

    public int ParticipantCount { get; set; }

    public int ReviewsEach { get; set; }

    /// <summary>Total reviews that could not be allocated because the cohort was too small.</summary>
    public int ShortfallCount { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One reviewer → reviewee-submission assignment within a run.</summary>
public sealed class PeerAllocationPair
{
    public int Id { get; set; }

    public int RunId { get; set; }

    public PeerAllocationRun? Run { get; set; }

    public int ConfigId { get; set; }

    public string ReviewerId { get; set; } = string.Empty;

    public int RevieweeSubmissionId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A submitted peer assessment for one allocation (one per assessor per reviewed submission).</summary>
public sealed class PeerReviewAssessment
{
    public int Id { get; set; }

    public int ConfigId { get; set; }

    public string AssessorId { get; set; } = string.Empty;

    public int RevieweeSubmissionId { get; set; }

    /// <summary>Sum of answer scores at submit time (denormalized for result math).</summary>
    public int TotalScore { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PeerAssessmentAnswer> Answers { get; set; } = new List<PeerAssessmentAnswer>();
}

/// <summary>Scores for one rubric question; prompt and max points are snapshotted so later edits cannot rewrite history.</summary>
public sealed class PeerAssessmentAnswer
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public PeerReviewAssessment? Assessment { get; set; }

    public int QuestionId { get; set; }

    public string PromptSnapshot { get; set; } = string.Empty;

    public int MaxPoints { get; set; }

    public int Score { get; set; }

    public string? Comment { get; set; }
}

/// <summary>The computed and/or overridden final score for one participant.</summary>
public sealed class PeerReviewResult
{
    public int Id { get; set; }

    public int ConfigId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    /// <summary>Server-computed score on the 0-100 scale under the configured strategy (null when inputs were missing).</summary>
    public int? ComputedScore { get; set; }

    /// <summary>Basis of the computation, e.g. "instructor", "peer", "instructor+peer".</summary>
    public string Basis { get; set; } = string.Empty;

    public int? OverrideScore { get; set; }

    public string? OverrideBy { get; set; }

    public DateTime? OverrideAt { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
