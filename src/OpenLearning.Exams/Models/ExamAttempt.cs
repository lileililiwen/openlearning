using OpenLearning.Auth.Models;

namespace OpenLearning.Exams.Models;

public enum ExamAttemptStatus
{
    InProgress = 0,
    Completed = 1,
}

/// <summary>
/// One student's attempt at an exam. Created when the student starts the exam
/// and finalized on submit or timeout.
/// </summary>
public class ExamAttempt
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public Exam? Exam { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    public int Score { get; set; }

    public int MaxScore { get; set; }

    public int Percent { get; set; }

    public bool Passed { get; set; }

    /// <summary>Number of times the student left the exam page (soft anti-cheat signal).</summary>
    public int ScreenSwitchCount { get; set; }

    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;

    public ICollection<ExamAttemptAnswer> Answers { get; set; } = new List<ExamAttemptAnswer>();
}
