namespace OpenLearning.Assessments.Models;

public class QuizAttemptAnswer
{
    public int Id { get; set; }

    public int AttemptId { get; set; }

    public QuizAttempt? Attempt { get; set; }

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    public int AnswerOptionId { get; set; }

    public AnswerOption? AnswerOption { get; set; }

    public bool IsCorrect { get; set; }
}
