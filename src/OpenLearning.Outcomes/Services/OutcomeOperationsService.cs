using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Outcomes.Models;

namespace OpenLearning.Outcomes.Services;

/// <summary>One recorded activity result used as input to mastery calculation.</summary>
public sealed class ActivityResult
{
    public int ActivityId { get; init; }

    public string ActivityType { get; init; } = string.Empty;

    /// <summary>Score as a fraction in the range 0..1.</summary>
    public double ScoreFraction { get; init; }

    public int Attempts { get; init; }

    public DateTime? LastAttemptAt { get; init; }
}

/// <summary>Explainable, role-scoped review signal for a learner against an outcome.</summary>
public sealed class InterventionSignal
{
    public string Rule { get; init; } = string.Empty;

    public string Evidence { get; init; } = string.Empty;

    public DateTime EvaluatedAt { get; init; }
}

/// <summary>The computed mastery for a learner against one outcome.</summary>
public sealed class MasteryCalculation
{
    public double Score { get; init; }

    public MasteryState State { get; init; }

    public int CalculationVersion { get; init; }

    public string SourceJson { get; init; } = "{}";
}

/// <summary>Mastery plus the explainable intervention signals for one learner/outcome pair.</summary>
public sealed class OutcomeEvaluation
{
    public MasteryCalculation Mastery { get; init; } = new();

    public List<InterventionSignal> Signals { get; init; } = new();
}

/// <summary>One field-level validation failure for an outcome or mapping change.</summary>
public sealed class MappingValidationError
{
    public string Field { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

/// <summary>Outcome of creating an outcome or adding a mapping.</summary>
public sealed class OutcomeMutationResult
{
    public bool Succeeded { get; init; }

    public List<MappingValidationError> Errors { get; init; } = new();

    public CourseOutcome? Outcome { get; init; }

    public OutcomeActivityMapping? Mapping { get; init; }
}

/// <summary>Request to declare a new course outcome.</summary>
public sealed class CreateOutcomeRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public double MasteryThreshold { get; init; } = 0.7;
}

/// <summary>Request to map an outcome to one assessable activity.</summary>
public sealed class AddMappingRequest
{
    public int OutcomeId { get; init; }

    public string ActivityType { get; init; } = string.Empty;

    public int ActivityId { get; init; }

    public double Weight { get; init; } = 1.0;
}

/// <summary>
/// Supplies recorded activity results for a learner in a course. Implemented at the
/// composition root (Web) so the Outcomes module stays free of Assessments/Exams deps.
/// </summary>
public interface IOutcomeActivitySource
{
    Task<IReadOnlyList<ActivityResult>> GetResultsAsync(int courseId, string learnerId, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when the actor is not the owning instructor of the outcome.</summary>
public sealed class UnauthorizedOutcomeOperationException : Exception
{
    public UnauthorizedOutcomeOperationException()
    {
    }

    public UnauthorizedOutcomeOperationException(string message)
        : base(message)
    {
    }

    public UnauthorizedOutcomeOperationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Deterministic, explainable outcome mastery operations. Owners map outcomes to
/// assessable activities; mastery is a versioned weighted rule; instructors see
/// role-scoped intervention signals. Learners never mutate outcomes.
/// </summary>
public sealed class OutcomeOperationsService
{
    /// <summary>Current version of the deterministic mastery rule.</summary>
    public const int CurrentCalculationVersion = 1;

    private static readonly string[] _knownActivityTypes = { "Quiz", "Assignment", "Exam" };

    private readonly DbContext _db;
    private readonly IOutcomeActivitySource? _activitySource;

    public OutcomeOperationsService(DbContext db, IOutcomeActivitySource? activitySource = null)
    {
        _db = db;
        _activitySource = activitySource;
    }

    /// <summary>Declares a new course outcome (owner only).</summary>
    public async Task<OutcomeMutationResult> CreateOutcomeAsync(int courseId, string actorId, CreateOutcomeRequest request)
    {
        var errors = new List<MappingValidationError>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new MappingValidationError { Field = nameof(request.Title), Code = "Required", Message = "Outcome title is required." });
        }

        if (request.MasteryThreshold <= 0 || request.MasteryThreshold > 1)
        {
            errors.Add(new MappingValidationError
            {
                Field = nameof(request.MasteryThreshold),
                Code = "Range",
                Message = "Mastery threshold must be greater than 0 and at most 1.",
            });
        }

        if (errors.Count > 0)
        {
            return new OutcomeMutationResult { Succeeded = false, Errors = errors };
        }

        var outcome = new CourseOutcome
        {
            CourseId = courseId,
            OwnerId = actorId,
            Title = request.Title,
            Description = request.Description,
            MasteryThreshold = request.MasteryThreshold,
        };

        _db.Set<CourseOutcome>().Add(outcome);
        await _db.SaveChangesAsync();
        return new OutcomeMutationResult { Succeeded = true, Outcome = outcome };
    }

    /// <summary>Maps an outcome to one assessable activity (owner only); rejects invalid mappings.</summary>
    public async Task<OutcomeMutationResult> AddOutcomeActivityMappingAsync(int courseId, string actorId, AddMappingRequest request)
    {
        var outcome = await GetOwnedOutcomeAsync(courseId, request.OutcomeId, actorId);

        var errors = new List<MappingValidationError>();

        if (outcome is null)
        {
            errors.Add(new MappingValidationError { Field = nameof(request.OutcomeId), Code = "NotFound", Message = "Outcome not found or not owned by actor." });
            return new OutcomeMutationResult { Succeeded = false, Errors = errors };
        }

        if (Array.IndexOf(_knownActivityTypes, request.ActivityType) < 0)
        {
            errors.Add(new MappingValidationError
            {
                Field = nameof(request.ActivityType),
                Code = "InvalidActivityType",
                Message = "Activity type must be one of: Quiz, Assignment, Exam.",
            });
        }

        if (request.ActivityId <= 0)
        {
            errors.Add(new MappingValidationError { Field = nameof(request.ActivityId), Code = "Required", Message = "Activity id is required." });
        }

        if (request.Weight <= 0)
        {
            errors.Add(new MappingValidationError { Field = nameof(request.Weight), Code = "Positive", Message = "Mapping weight must be greater than 0." });
        }

        var duplicate = await _db.Set<OutcomeActivityMapping>().AsNoTracking()
            .AnyAsync(m => m.OutcomeId == request.OutcomeId && m.ActivityId == request.ActivityId);
        if (duplicate)
        {
            errors.Add(new MappingValidationError
            {
                Field = nameof(request.ActivityId),
                Code = "Duplicate",
                Message = "This activity is already mapped to the outcome.",
            });
        }

        if (errors.Count > 0)
        {
            return new OutcomeMutationResult { Succeeded = false, Errors = errors };
        }

        var mapping = new OutcomeActivityMapping
        {
            CourseId = courseId,
            OutcomeId = request.OutcomeId,
            ActivityId = request.ActivityId,
            ActivityType = request.ActivityType,
            Weight = request.Weight,
        };

        _db.Set<OutcomeActivityMapping>().Add(mapping);
        await _db.SaveChangesAsync();
        return new OutcomeMutationResult { Succeeded = true, Mapping = mapping };
    }

    /// <summary>Returns the course outcomes together with their activity mappings.</summary>
    public async Task<List<(CourseOutcome Outcome, List<OutcomeActivityMapping> Mappings)>> GetOutcomesWithMappingsAsync(int courseId)
    {
        var outcomes = await _db.Set<CourseOutcome>().AsNoTracking()
            .Where(o => o.CourseId == courseId)
            .OrderBy(o => o.Id)
            .ToListAsync();

        var mappings = await _db.Set<OutcomeActivityMapping>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .ToListAsync();

        var result = new List<(CourseOutcome, List<OutcomeActivityMapping>)>();
        foreach (var outcome in outcomes)
        {
            var owned = mappings.Where(m => m.OutcomeId == outcome.Id).ToList();
            result.Add((outcome, owned));
        }

        return result;
    }

    /// <summary>
    /// Calculates and persists mastery for a learner using explicit results (owner only).
    /// Recalculation is idempotent: the single current <see cref="MasteryResult"/> row is upserted.
    /// </summary>
    public async Task<OutcomeEvaluation> CalculateWithAsync(
        int courseId, int outcomeId, string learnerId, IReadOnlyList<ActivityResult> results, string actorId)
    {
        var outcome = await GetOwnedOutcomeAsync(courseId, outcomeId, actorId);
        if (outcome is null)
        {
            throw new UnauthorizedOutcomeOperationException($"Actor '{actorId}' is not authorized for outcome {outcomeId}.");
        }

        var mappings = await _db.Set<OutcomeActivityMapping>().AsNoTracking()
            .Where(m => m.OutcomeId == outcomeId)
            .ToListAsync();

        var evaluation = Evaluate(outcome, mappings, results);
        await UpsertMasteryAsync(courseId, outcomeId, learnerId, evaluation.Mastery);
        return evaluation;
    }

    /// <summary>
    /// Recalculates and persists mastery using the registered <see cref="IOutcomeActivitySource"/>
    /// (owner only). Falls back to no results when no source is registered.
    /// </summary>
    public async Task<OutcomeEvaluation> RecalculateAsync(int courseId, int outcomeId, string learnerId, string actorId)
    {
        var results = await FetchResultsAsync(courseId, learnerId);
        return await CalculateWithAsync(courseId, outcomeId, learnerId, results, actorId);
    }

    /// <summary>
    /// Evaluates mastery and signals without persisting, using the registered activity source.
    /// Safe for learner self-view (no ownership check, no writes).
    /// </summary>
    public async Task<OutcomeEvaluation> EvaluateAsync(int courseId, int outcomeId, string learnerId)
    {
        var outcome = await _db.Set<CourseOutcome>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == outcomeId && o.CourseId == courseId);
        if (outcome is null)
        {
            return new OutcomeEvaluation();
        }

        var mappings = await _db.Set<OutcomeActivityMapping>().AsNoTracking()
            .Where(m => m.OutcomeId == outcomeId)
            .ToListAsync();

        var results = await FetchResultsAsync(courseId, learnerId);
        return Evaluate(outcome, mappings, results);
    }

    /// <summary>Returns the persisted current mastery result (read-only, for learner self-view).</summary>
    public async Task<MasteryResult?> GetCurrentMasteryAsync(int courseId, int outcomeId, string learnerId)
    {
        return await _db.Set<MasteryResult>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.OutcomeId == outcomeId && r.LearnerId == learnerId);
    }

    private async Task<IReadOnlyList<ActivityResult>> FetchResultsAsync(int courseId, string learnerId)
    {
        if (_activitySource is null)
        {
            return Array.Empty<ActivityResult>();
        }

        return await _activitySource.GetResultsAsync(courseId, learnerId);
    }

    private static OutcomeEvaluation Evaluate(CourseOutcome outcome, List<OutcomeActivityMapping> mappings, IReadOnlyList<ActivityResult> results)
    {
        var byActivity = new Dictionary<int, OutcomeActivityMapping>();
        foreach (var mapping in mappings)
        {
            byActivity[mapping.ActivityId] = mapping;
        }

        var matched = new List<(OutcomeActivityMapping Mapping, ActivityResult Result)>();
        foreach (var result in results)
        {
            if (byActivity.TryGetValue(result.ActivityId, out var mapping))
            {
                matched.Add((mapping, result));
            }
        }

        var weightSum = 0.0;
        var weightedScore = 0.0;
        foreach (var (mapping, result) in matched)
        {
            var fraction = Math.Clamp(result.ScoreFraction, 0, 1);
            weightSum += mapping.Weight;
            weightedScore += mapping.Weight * fraction;
        }

        var score = weightSum > 0 ? weightedScore / weightSum : 0.0;
        var now = DateTime.UtcNow;

        var state = MasteryState.NotStarted;
        if (matched.Count > 0)
        {
            state = score >= outcome.MasteryThreshold ? MasteryState.Mastered : MasteryState.Developing;
        }

        var signals = new List<InterventionSignal>();
        var attemptedIds = new HashSet<int>(matched.Select(x => x.Result.ActivityId));

        foreach (var mapping in mappings.Where(m => !attemptedIds.Contains(m.ActivityId)))
        {
            signals.Add(new InterventionSignal
            {
                Rule = "MissingWork",
                Evidence = $"Outcome '{outcome.Title}' activity {mapping.ActivityType}#{mapping.ActivityId} has no submission.",
                EvaluatedAt = now,
            });
        }

        var reviewWorthy = false;
        foreach (var (_, result) in matched)
        {
            var fraction = Math.Clamp(result.ScoreFraction, 0, 1);
            if (result.Attempts >= 3 && fraction < 0.5)
            {
                reviewWorthy = true;
                signals.Add(new InterventionSignal
                {
                    Rule = "RepeatedLowAttempts",
                    Evidence = $"Activity {result.ActivityType}#{result.ActivityId} attempted {result.Attempts} times, best {FormatPercent(fraction)}%.",
                    EvaluatedAt = now,
                });
            }

            if (result.LastAttemptAt is { } lastAttempt && lastAttempt < now.AddDays(-30))
            {
                reviewWorthy = true;
                signals.Add(new InterventionSignal
                {
                    Rule = "StaleProgress",
                    Evidence = $"Activity {result.ActivityType}#{result.ActivityId} last attempted {lastAttempt:u}; progress is stale.",
                    EvaluatedAt = now,
                });
            }
        }

        if (matched.Count > 0 && score < outcome.MasteryThreshold)
        {
            signals.Add(new InterventionSignal
            {
                Rule = "UnmetOutcome",
                Evidence = $"Weighted score {FormatPercent(score)}% is below the mastery threshold {FormatPercent(outcome.MasteryThreshold)}%.",
                EvaluatedAt = now,
            });
        }

        if (state == MasteryState.Developing && reviewWorthy)
        {
            state = MasteryState.NeedsReview;
        }

        var source = new
        {
            CalculationVersion = CurrentCalculationVersion,
            Threshold = outcome.MasteryThreshold,
            Mappings = mappings.Select(m => new { m.ActivityId, m.ActivityType, m.Weight }).ToList(),
            Results = matched.Select(x => new { x.Result.ActivityId, x.Result.ActivityType, Fraction = Math.Clamp(x.Result.ScoreFraction, 0, 1), x.Result.Attempts }).ToList(),
            Score = score,
        };

        var mastery = new MasteryCalculation
        {
            Score = score,
            State = state,
            CalculationVersion = CurrentCalculationVersion,
            SourceJson = JsonSerializer.Serialize(source),
        };

        return new OutcomeEvaluation { Mastery = mastery, Signals = signals };
    }

    private async Task<CourseOutcome?> GetOwnedOutcomeAsync(int courseId, int outcomeId, string actorId)
    {
        var outcome = await _db.Set<CourseOutcome>()
            .FirstOrDefaultAsync(o => o.Id == outcomeId && o.CourseId == courseId);
        if (outcome is null || outcome.OwnerId != actorId)
        {
            return null;
        }

        return outcome;
    }

    private async Task UpsertMasteryAsync(int courseId, int outcomeId, string learnerId, MasteryCalculation mastery)
    {
        var existing = await _db.Set<MasteryResult>()
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.OutcomeId == outcomeId && r.LearnerId == learnerId);

        if (existing is null)
        {
            _db.Set<MasteryResult>().Add(new MasteryResult
            {
                CourseId = courseId,
                OutcomeId = outcomeId,
                LearnerId = learnerId,
                CalculationVersion = mastery.CalculationVersion,
                Score = mastery.Score,
                State = mastery.State,
                SourceJson = mastery.SourceJson,
            });
        }
        else
        {
            existing.CalculationVersion = mastery.CalculationVersion;
            existing.Score = mastery.Score;
            existing.State = mastery.State;
            existing.SourceJson = mastery.SourceJson;
            existing.CalculatedAt = DateTime.UtcNow;
            _db.Set<MasteryResult>().Update(existing);
        }

        await _db.SaveChangesAsync();
    }

    private static string FormatPercent(double fraction)
    {
        return $"{Math.Round(fraction * 100, 1)}";
    }
}
