namespace OpenLearning.Assessments.Models;

public enum IncorrectAnswerSource
{
    Quiz = 0,
    Exam = 1,
}

/// <summary>One wrong answer logged from a quiz/exam attempt for deliberate practice.</summary>
public class IncorrectAnswer
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    public int CourseId { get; set; }

    /// <summary>Human-readable answer the student gave.</summary>
    public string ChosenAnswer { get; set; } = string.Empty;

    /// <summary>Human-readable correct answer (empty for manual-graded types).</summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    public IncorrectAnswerSource SourceType { get; set; }

    public int SourceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the student answers this question correctly in practice.</summary>
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>A question bookmarked for later review.</summary>
public class BookmarkedQuestion
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
