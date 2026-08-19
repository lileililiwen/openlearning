namespace OpenLearning.Assessments.Models;

public class Question
{
    public int Id { get; set; }

    /// <summary>Container quiz; null when the question belongs to an exam instead.</summary>
    public int? QuizId { get; set; }

    public Quiz? Quiz { get; set; }

    /// <summary>Container exam; null when the question belongs to a quiz instead.</summary>
    public int? ExamId { get; set; }

    public string Text { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;

    public int OrderIndex { get; set; }

    public int Points { get; set; } = 1;

    /// <summary>True for rows in the central question bank (no quiz/exam association).</summary>
    public bool IsBank { get; set; }

    /// <summary>Free-form topic/tag for bank questions.</summary>
    public string? BankTopic { get; set; }

    /// <summary>Set when an admin archives a bank question (hidden from import/search).</summary>
    public DateTime? ArchivedAt { get; set; }

    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}
