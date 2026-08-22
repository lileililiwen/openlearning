using OpenLearning.Auth.Models;

namespace OpenLearning.Exams.Models;

/// <summary>
/// Authorized learner adjustment, snapshotted onto an attempt without exposing
/// any diagnosis. Only the operational adjustment (extra time, breaks, relaxed
/// thresholds) is visible to exam staff.
/// </summary>
public class LearnerAccommodation
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    /// <summary>Attempt this accommodation was applied to (set when the attempt starts).</summary>
    public int? AttemptId { get; set; }

    public ExamAttempt? Attempt { get; set; }

    /// <summary>Extra minutes added to the server deadline.</summary>
    public int ExtraMinutes { get; set; }

    public int AllowedBreaks { get; set; }

    /// <summary>Relaxed event-count thresholds applied during risk scoring.</summary>
    public int RelaxedVisibilityThreshold { get; set; }

    public int RelaxedCopyPasteThreshold { get; set; }

    public string? GrantedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
