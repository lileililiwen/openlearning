using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;

namespace OpenLearning.QuestionIO.Services;

/// <summary>One data row read from the Excel template (RowIndex is the original sheet row).</summary>
internal sealed record ParsedQuestionRow(
    int RowIndex,
    string? RowId,
    string? QuestionTypeText,
    string? Stem,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    string? CorrectAnswer,
    string? Explanation,
    string? DifficultyText,
    string? KnowledgeTag,
    string? BankTopic);

/// <summary>Validated values ready to persist.</summary>
internal sealed record QuestionRowInput(
    string? RowId,
    string Stem,
    QuestionType QuestionType,
    List<AnswerOptionInput> Options,
    string? Explanation,
    QuestionDifficulty Difficulty,
    string? KnowledgeTag);
