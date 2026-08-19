namespace OpenLearning.Assessments.Models;

public class QuizAttemptAnswer
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public QuizAttempt? Attempt { get; set; }

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    /// <summary>Selected option for single-choice and true/false; null for other types.</summary>
    public int? AnswerOptionId { get; set; }

    public AnswerOption? AnswerOption { get; set; }

    /// <summary>Comma-separated selected option ids for multiple-choice.</summary>
    public string? SelectedOptionIds { get; set; }

    /// <summary>Free text for fill-in-the-blank and short-answer (and true/false as "true"/"false" fallback).</summary>
    public string? TextAnswer { get; set; }

    /// <summary>Uploaded file URL for file-upload answers (from the storage module).</summary>
    public string? FileAnswerUrl { get; set; }

    /// <summary>Auto-scored objective correctness; manual answers are false until graded.</summary>
    public bool IsCorrect { get; set; }

    /// <summary>True once an instructor has graded a manual (short-answer/file-upload) answer.</summary>
    public bool IsGraded { get; set; }

    /// <summary>Instructor-assigned score in points for a manual answer.</summary>
    public int? GradedScore { get; set; }

    public string? GradingFeedback { get; set; }
}
