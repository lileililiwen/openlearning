namespace OpenLearning.Surveys.Models;

public enum SurveyQuestionType
{
    SingleChoice = 0,
    MultipleChoice = 1,
    RatingScale = 2,
    OpenText = 3,
}

public enum SurveyScope
{
    Course = 0,
    Platform = 1,
}

/// <summary>An instructor (course scope) or admin (platform scope) questionnaire.</summary>
public sealed class Survey
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public SurveyScope Scope { get; set; }

    /// <summary>Set when Scope is Course; null for platform-wide surveys.</summary>
    public int? CourseId { get; set; }

    /// <summary>When true, responses are stored without respondent identity linkage.</summary>
    public bool IsAnonymous { get; set; }

    /// <summary>When true the author may view aggregate results before the survey closes.</summary>
    public bool AllowLiveResults { get; set; }

    public DateTime? OpensAt { get; set; }

    public DateTime? ClosesAt { get; set; }

    /// <summary>Per-survey salt used to derive the duplicate-prevention token for anonymous surveys.</summary>
    public byte[] TokenSalt { get; set; } = new byte[32];

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
}

public sealed class SurveyQuestion
{
    public int Id { get; set; }

    public int SurveyId { get; set; }

    public Survey? Survey { get; set; }

    public int SortOrder { get; set; }

    public SurveyQuestionType Type { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public ICollection<SurveyQuestionOption> Options { get; set; } = new List<SurveyQuestionOption>();
}

/// <summary>One selectable answer for single/multiple-choice questions.</summary>
public sealed class SurveyQuestionOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public SurveyQuestion? Question { get; set; }

    public int SortOrder { get; set; }

    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// One submitted questionnaire. For anonymous surveys RespondentUserId stays
/// null and only the keyed RespondentToken is retained.
/// </summary>
public sealed class SurveyResponse
{
    public int Id { get; set; }

    public int SurveyId { get; set; }

    public Survey? Survey { get; set; }

    public string? RespondentUserId { get; set; }

    /// <summary>HMAC(salt, userId) for anonymous surveys; plain userId for attributed ones.</summary>
    public string RespondentToken { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}

public sealed class SurveyAnswer
{
    public int Id { get; set; }

    public int ResponseId { get; set; }

    public SurveyResponse? Response { get; set; }

    public int QuestionId { get; set; }

    /// <summary>Selected option for single/multiple-choice questions.</summary>
    public int? OptionId { get; set; }

    /// <summary>1..5 value for rating-scale questions.</summary>
    public int? RatingValue { get; set; }

    /// <summary>Free text for open-text questions.</summary>
    public string? TextValue { get; set; }
}
