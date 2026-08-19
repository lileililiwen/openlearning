using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Services;

/// <summary>
/// Question-types scoring shared by quizzes (AttemptService) and exams
/// (OpenLearning.Exams.ExamService) so both produce identical results.
/// Objective types auto-score; short-answer and file-upload answers are manual
/// and only count toward the total once graded.
/// </summary>
public static class QuestionScoring
{
    /// <summary>Scored values for one question answer; callers persist these onto their own answer entity.</summary>
    public sealed record ScoredAnswer(
        Question Question,
        int? OptionId,
        string? SelectedOptionIds,
        string? TextAnswer,
        string? FileAnswerUrl,
        bool IsCorrect,
        bool IsGraded = false,
        int? GradedScore = null);

    /// <summary>Computes correctness per question type; manual types are never auto-correct.</summary>
    public static ScoredAnswer Score(
        Question question, int? optionId, string? selectedOptionIds, string? textAnswer, string? fileAnswerUrl)
    {
        var isCorrect = question.QuestionType switch
        {
            QuestionType.SingleChoice or QuestionType.TrueFalse =>
                question.AnswerOptions.Any(o => o.Id == optionId && o.IsCorrect),
            QuestionType.MultipleChoice =>
                ParseIds(selectedOptionIds).SetEquals(
                    question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet()),
            QuestionType.FillBlank => IsFillBlankMatch(question, textAnswer),
            _ => false,
        };

        return new ScoredAnswer(question, optionId, selectedOptionIds, textAnswer, fileAnswerUrl, isCorrect);
    }

    /// <summary>
    /// Auto-scored objective questions count toward Score/MaxScore immediately;
    /// manual questions only count once graded.
    /// </summary>
    public static (int Score, int MaxScore) ComputeScores(IEnumerable<ScoredAnswer> answers)
    {
        var score = 0;
        var maxScore = 0;
        foreach (var answer in answers)
        {
            var question = answer.Question;
            if (question.QuestionType is QuestionType.ShortAnswer or QuestionType.FileUpload)
            {
                if (answer.IsGraded)
                {
                    maxScore += question.Points;
                    score += answer.GradedScore ?? 0;
                }

                continue;
            }

            maxScore += question.Points;
            if (answer.IsCorrect)
            {
                score += question.Points;
            }
        }

        return (score, maxScore);
    }

    public static bool IsManual(QuestionType type)
    {
        return type is QuestionType.ShortAnswer or QuestionType.FileUpload;
    }

    private static HashSet<int> ParseIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<int>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .ToHashSet();
    }

    private static string Normalize(string value)
    {
        return string.Concat(value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    private static bool IsFillBlankMatch(Question question, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = Normalize(text);
        return question.AnswerOptions
            .Where(o => o.IsCorrect)
            .Any(o => Normalize(o.Text) == normalized);
    }
}
