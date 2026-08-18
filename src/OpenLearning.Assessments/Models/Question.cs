namespace OpenLearning.Assessments.Models;

public class Question
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public Quiz? Quiz { get; set; }

    public string Text { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public int Points { get; set; } = 1;

    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}
