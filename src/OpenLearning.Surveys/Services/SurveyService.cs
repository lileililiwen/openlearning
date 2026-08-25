using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Enrollment.Services;
using OpenLearning.Surveys.Models;

namespace OpenLearning.Surveys.Services;

/// <summary>
/// Non-graded surveys and polls: authoring with four question types, windowed
/// one-response collection, structural anonymity (no respondent identity stored),
/// and policy-gated aggregate results. Participation never touches grades,
/// progress, credits, certificates, or gamification scoring.
/// </summary>
public class SurveyService
{
    private const int _ratingMin = 1;
    private const int _ratingMax = 5;

    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;

    public SurveyService(DbContext db, EnrollmentService enrollments)
    {
        _db = db;
        _enrollments = enrollments;
    }

    // ===== Authoring =====

    public sealed record QuestionInput(
        SurveyQuestionType Type,
        string Prompt,
        bool IsRequired,
        IReadOnlyList<string> Options);

    public async Task<(bool Ok, string? Error)> CreateAsync(
        string authorId, bool isAdmin, SurveyScope scope, int? courseId,
        string title, string description, bool isAnonymous, bool allowLiveResults,
        DateTime? opensAt, DateTime? closesAt, IReadOnlyList<QuestionInput> questions)
    {
        if (!isAdmin && scope == SurveyScope.Platform)
        {
            return (false, "Only Admins can create platform-wide surveys.");
        }

        if (scope == SurveyScope.Course && courseId is null)
        {
            return (false, "A course is required for course-scope surveys.");
        }

        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is 0 or > 200)
        {
            return (false, "Survey title is required (200 characters or fewer).");
        }

        var open = Normalize(opensAt);
        var close = Normalize(closesAt);
        if (open is not null && close is not null && open >= close)
        {
            return (false, "The close time must be after the open time.");
        }

        if (questions.Count is < 1 or > 20)
        {
            return (false, "Provide between 1 and 20 questions.");
        }

        foreach (var question in questions)
        {
            if (string.IsNullOrWhiteSpace(question.Prompt) || question.Prompt.Trim().Length > 500)
            {
                return (false, "Each question needs a prompt of 500 characters or fewer.");
            }

            var needsOptions = question.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice;
            var options = question.Options
                .Select(o => o?.Trim() ?? string.Empty)
                .Where(o => o.Length > 0)
                .ToList();
            if (needsOptions && options.Count is < 2 or > 10)
            {
                return (false, "Choice questions need between 2 and 10 answer options.");
            }

            if (!needsOptions && options.Count > 0)
            {
                return (false, "Only choice questions take answer options.");
            }
        }

        var salt = RandomNumberGenerator.GetBytes(32);
        var survey = new Survey
        {
            Title = trimmedTitle,
            Description = description?.Trim() ?? string.Empty,
            Scope = scope,
            CourseId = scope == SurveyScope.Course ? courseId : null,
            IsAnonymous = isAnonymous,
            AllowLiveResults = allowLiveResults,
            OpensAt = open,
            ClosesAt = close,
            TokenSalt = salt,
            CreatedBy = authorId,
        };

        for (var i = 0; i < questions.Count; i++)
        {
            var input = questions[i];
            var question = new SurveyQuestion
            {
                SortOrder = i + 1,
                Type = input.Type,
                Prompt = input.Prompt.Trim(),
                IsRequired = input.IsRequired,
            };

            if (input.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice)
            {
                var options = input.Options
                    .Select(o => o?.Trim() ?? string.Empty)
                    .Where(o => o.Length > 0)
                    .ToList();
                for (var j = 0; j < options.Count; j++)
                {
                    question.Options.Add(new SurveyQuestionOption { SortOrder = j + 1, Text = options[j] });
                }
            }

            survey.Questions.Add(question);
        }

        _db.Set<Survey>().Add(survey);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<List<Survey>> GetForCourseAsync(int? courseId)
    {
        return _db.Set<Survey>().AsNoTracking()
            .Include(s => s.Questions)
            .ThenInclude(q => q.Options)
            .Where(s => s.Scope == SurveyScope.Course && s.CourseId == courseId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public Task<List<Survey>> GetPlatformSurveysAsync()
    {
        return _db.Set<Survey>().AsNoTracking()
            .Include(s => s.Questions)
            .ThenInclude(q => q.Options)
            .Where(s => s.Scope == SurveyScope.Platform)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public Task<Survey?> GetAsync(int id)
    {
        return _db.Set<Survey>().AsNoTracking()
            .Include(s => s.Questions)
            .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    public static bool IsOpen(Survey survey, DateTime now)
    {
        if (survey.OpensAt is DateTime opens && now < opens)
        {
            return false;
        }

        return survey.ClosesAt is not DateTime closes || now <= closes;
    }

    private static DateTime? Normalize(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
    }

    public static bool IsClosed(Survey survey, DateTime now)
    {
        return survey.ClosesAt is DateTime closes && now > closes;
    }

    public async Task<bool> CanManageAsync(Survey survey, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        if (survey.Scope != SurveyScope.Course || survey.CourseId is null)
        {
            return false;
        }

        return await _db.Set<global::OpenLearning.CourseManagement.Models.Course>().AsNoTracking()
            .AnyAsync(c => c.Id == survey.CourseId && c.InstructorId == userId);
    }

    // ===== Eligibility and tokens =====

    public async Task<bool> IsEligibleAsync(Survey survey, string userId)
    {
        if (survey.Scope == SurveyScope.Platform)
        {
            return true;
        }

        return survey.CourseId is not null &&
               await _enrollments.IsEnrolledAsync(userId, survey.CourseId.Value);
    }

    /// <summary>
    /// Duplicate-prevention token. Attributed surveys use the user id directly;
    /// anonymous surveys store only a keyed hash so no identity linkage exists.
    /// </summary>
    public static string DeriveToken(Survey survey, string userId)
    {
        if (!survey.IsAnonymous)
        {
            return userId;
        }

        using var hmac = new HMACSHA256(survey.TokenSalt);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hash)[..64].ToLowerInvariant();
    }

    public async Task<bool> HasRespondedAsync(Survey survey, string userId)
    {
        var token = DeriveToken(survey, userId);
        return await _db.Set<SurveyResponse>().AsNoTracking()
            .AnyAsync(r => r.SurveyId == survey.Id && r.RespondentToken == token);
    }

    // ===== Response submission =====

    public sealed record AnswerInput(int QuestionId, List<int> OptionIds, int? RatingValue, string? TextValue);

    public async Task<(bool Ok, string? Error)> SubmitAsync(
        int surveyId, string userId, IReadOnlyDictionary<int, AnswerInput> answers)
    {
        var survey = await _db.Set<Survey>().AsNoTracking()
            .Include(s => s.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.Id == surveyId);
        if (survey is null)
        {
            return (false, "Survey not found.");
        }

        if (!IsOpen(survey, DateTime.UtcNow))
        {
            return (false, "This survey is not open for responses.");
        }

        if (!await IsEligibleAsync(survey, userId))
        {
            return (false, "You are not eligible to respond to this survey.");
        }

        var token = DeriveToken(survey, userId);
        var alreadyResponded = await _db.Set<SurveyResponse>().AsNoTracking()
            .AnyAsync(r => r.SurveyId == survey.Id && r.RespondentToken == token);
        if (alreadyResponded)
        {
            return (false, "You have already responded to this survey.");
        }

        var missingRequired = survey.Questions
            .Any(q => q.IsRequired && !answers.ContainsKey(q.Id));
        if (missingRequired)
        {
            return (false, "Answer every required question before submitting.");
        }

        var response = new SurveyResponse
        {
            SurveyId = survey.Id,
            RespondentUserId = survey.IsAnonymous ? null : userId,
            RespondentToken = token,
        };

        foreach (var question in survey.Questions)
        {
            if (!answers.TryGetValue(question.Id, out var input))
            {
                continue;
            }

            switch (question.Type)
            {
                case SurveyQuestionType.SingleChoice:
                    if (input.OptionIds.Count != 1 ||
                        !question.Options.Any(o => o.Id == input.OptionIds[0]))
                    {
                        return (false, $"Choose exactly one option for \"{question.Prompt}\".");
                    }

                    response.Answers.Add(new SurveyAnswer { QuestionId = question.Id, OptionId = input.OptionIds[0] });
                    break;

                case SurveyQuestionType.MultipleChoice:
                    if (input.OptionIds.Count == 0 ||
                        input.OptionIds.Any(id => question.Options.All(o => o.Id != id)))
                    {
                        return (false, $"Choose at least one valid option for \"{question.Prompt}\".");
                    }

                    foreach (var answer in input.OptionIds.Distinct()
                                 .Select(optionId => new SurveyAnswer { QuestionId = question.Id, OptionId = optionId }))
                    {
                        response.Answers.Add(answer);
                    }

                    break;

                case SurveyQuestionType.RatingScale:
                    if (input.RatingValue is < _ratingMin or > _ratingMax)
                    {
                        return (false, $"Rating for \"{question.Prompt}\" must be between {_ratingMin} and {_ratingMax}.");
                    }

                    response.Answers.Add(new SurveyAnswer { QuestionId = question.Id, RatingValue = input.RatingValue });
                    break;

                case SurveyQuestionType.OpenText:
                    if (question.IsRequired && string.IsNullOrWhiteSpace(input.TextValue))
                    {
                        return (false, $"An answer is required for \"{question.Prompt}\".");
                    }

                    response.Answers.Add(new SurveyAnswer
                    {
                        QuestionId = question.Id,
                        TextValue = string.IsNullOrWhiteSpace(input.TextValue) ? null : input.TextValue.Trim(),
                    });
                    break;
            }
        }

        response.SubmittedAt = DateTime.UtcNow;
        _db.Set<SurveyResponse>().Add(response);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<int> CountResponsesAsync(int surveyId)
    {
        return _db.Set<SurveyResponse>().CountAsync(r => r.SurveyId == surveyId);
    }

    // ===== Results =====

    public sealed record ChoiceBucket(int OptionId, string Text, int Count);

    public sealed record QuestionResult(
        SurveyQuestion Question,
        List<ChoiceBucket> Choices,
        Dictionary<int, int> Ratings,
        double? AverageRating,
        List<string> OpenTexts);

    public sealed record SurveyResults(bool CanSeeAnswers, int ResponseCount, List<QuestionResult> Questions);

    /// <summary>
    /// Aggregate results. Answer content is revealed only after the survey
    /// closes unless live results were enabled; anonymous surveys expose
    /// aggregates only — never individual responses.
    /// </summary>
    public async Task<SurveyResults> GetResultsAsync(Survey survey, string requesterId, bool isAdmin)
    {
        var canManage = await CanManageAsync(survey, requesterId, isAdmin);
        var closed = IsClosed(survey, DateTime.UtcNow);
        var canSeeAnswers = canManage && (closed || survey.AllowLiveResults);

        var responseCount = await CountResponsesAsync(survey.Id);
        var results = new List<QuestionResult>();

        if (canSeeAnswers)
        {
            var answers = await _db.Set<SurveyAnswer>().AsNoTracking()
                .Where(a => a.Response!.SurveyId == survey.Id)
                .ToListAsync();

            foreach (var question in survey.Questions.OrderBy(q => q.SortOrder))
            {
                var forQuestion = answers.Where(a => a.QuestionId == question.Id).ToList();

                var choices = new List<ChoiceBucket>();
                var ratings = new Dictionary<int, int>();
                var texts = new List<string>();

                switch (question.Type)
                {
                    case SurveyQuestionType.SingleChoice:
                    case SurveyQuestionType.MultipleChoice:
                        choices = question.Options.OrderBy(o => o.SortOrder)
                            .Select(o => new ChoiceBucket(
                                o.Id,
                                o.Text,
                                forQuestion.Count(a => a.OptionId == o.Id)))
                            .ToList();
                        break;

                    case SurveyQuestionType.RatingScale:
                        ratings = forQuestion
                            .Where(a => a.RatingValue is not null)
                            .GroupBy(a => a.RatingValue!.Value)
                            .ToDictionary(g => g.Key, g => g.Count());
                        var ratingValues = forQuestion
                            .Where(a => a.RatingValue is not null)
                            .Select(a => (double)a.RatingValue!.Value)
                            .ToList();
                        var average = ratingValues.Count > 0 ? ratingValues.Average() : 0d;
                        results.Add(new QuestionResult(question, choices, ratings, average, texts));
                        continue;

                    case SurveyQuestionType.OpenText:
                        texts = forQuestion
                            .Where(a => !string.IsNullOrWhiteSpace(a.TextValue))
                            .Select(a => a.TextValue!.Trim())
                            .ToList();
                        break;
                }

                results.Add(new QuestionResult(question, choices, ratings, null, texts));
            }
        }

        return new SurveyResults(canSeeAnswers, responseCount, results);
    }
}
