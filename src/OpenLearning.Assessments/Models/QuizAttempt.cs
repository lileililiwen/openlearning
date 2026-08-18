using OpenLearning.Auth.Models;

namespace OpenLearning.Assessments.Models;

public class QuizAttempt
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public Quiz? Quiz { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int Score { get; set; }

    public int MaxScore { get; set; }

    public ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
}
