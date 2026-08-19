namespace OpenLearning.Assessments.Models;

/// <summary>Kinds of quiz questions; objective types auto-score, manual types are graded.</summary>
public enum QuestionType
{
    /// <summary>One correct answer from a set of options.</summary>
    SingleChoice = 0,

    /// <summary>One or more correct answers from a set of options.</summary>
    MultipleChoice = 1,

    /// <summary>True or false; rendered as two options.</summary>
    TrueFalse = 2,

    /// <summary>Free-text answer compared case-insensitively against acceptable answers.</summary>
    FillBlank = 3,

    /// <summary>Free-text answer graded manually by the instructor.</summary>
    ShortAnswer = 4,

    /// <summary>File upload answer graded manually by the instructor.</summary>
    FileUpload = 5,
}
