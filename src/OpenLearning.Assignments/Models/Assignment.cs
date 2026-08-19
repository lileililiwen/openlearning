namespace OpenLearning.Assignments.Models;

/// <summary>An open-ended assignment published by the course owner.</summary>
public class Assignment
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public DateTime? DueAt { get; set; }

    /// <summary>When false, a submission after grading is rejected.</summary>
    public bool AllowResubmitAfterGrading { get; set; }

    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}

/// <summary>A student's submission to an assignment (one per student).</summary>
public class AssignmentSubmission
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public Assignment Assignment { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    /// <summary>Stored file URL (from the storage module) when a file was uploaded.</summary>
    public string? FileUrl { get; set; }

    public DateTime SubmittedAt { get; set; }

    public int? Score { get; set; }

    public string? Feedback { get; set; }

    public DateTime? GradedAt { get; set; }

    public string? GradedBy { get; set; }
}
