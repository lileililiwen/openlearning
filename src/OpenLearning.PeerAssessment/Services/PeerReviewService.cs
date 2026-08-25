using Microsoft.EntityFrameworkCore;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Notifications.Services;
using OpenLearning.PeerAssessment.Models;

namespace OpenLearning.PeerAssessment.Services;

/// <summary>
/// Peer review configuration, deterministic self-free reviewer allocation,
/// rubric-gated assessment submission, and policy-driven final-score
/// combination with instructor override. Final scores live in this module;
/// assignment grade records are never written.
/// </summary>
public class PeerReviewService
{
    private readonly DbContext _db;
    private readonly AssignmentService _assignments;
    private readonly EnrollmentService _enrollments;
    private readonly UserService _users;
    private readonly NotificationService _notifications;

    public PeerReviewService(
        DbContext db,
        AssignmentService assignments,
        EnrollmentService enrollments,
        UserService users,
        NotificationService notifications)
    {
        _db = db;
        _assignments = assignments;
        _enrollments = enrollments;
        _users = users;
        _notifications = notifications;
    }

    // ===== Configuration =====

    public Task<PeerReviewConfig?> GetConfigAsync(int assignmentId)
    {
        return _db.Set<PeerReviewConfig>().AsNoTracking()
            .Include(c => c.RubricQuestions)
            .FirstOrDefaultAsync(c => c.AssignmentId == assignmentId);
    }

    public Task<PeerReviewConfig?> GetConfigByIdAsync(int configId)
    {
        return _db.Set<PeerReviewConfig>().AsNoTracking()
            .Include(c => c.RubricQuestions)
            .FirstOrDefaultAsync(c => c.Id == configId);
    }

    public static PeerReviewPhase GetPhase(PeerReviewConfig config, DateTime now)
    {
        if (now < config.ReviewOpensAt)
        {
            return PeerReviewPhase.Submission;
        }

        return now < config.ReviewClosesAt ? PeerReviewPhase.Review : PeerReviewPhase.Closed;
    }

    public async Task<(bool Ok, string? Error)> SaveConfigAsync(
        int assignmentId,
        int reviewsPerStudent,
        bool isAnonymous,
        PeerReviewStrategy strategy,
        int instructorWeightPercent,
        DateTime? reviewOpensAt,
        DateTime reviewClosesAt,
        List<(string Prompt, int MaxPoints)> rubric)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
        {
            return (false, "Assignment not found.");
        }

        if (reviewsPerStudent is < 1 or > 10)
        {
            return (false, "Reviews per student must be between 1 and 10.");
        }

        var opens = Normalize(reviewOpensAt) ?? DateTime.UtcNow;
        var closes = Normalize(reviewClosesAt);
        if (closes is null)
        {
            return (false, "A review close time is required.");
        }

        if (opens >= closes.Value)
        {
            return (false, "The review close time must be after the open time.");
        }

        if (strategy == PeerReviewStrategy.WeightedMix && instructorWeightPercent is < 0 or > 100)
        {
            return (false, "Instructor weight must be between 0 and 100 percent.");
        }

        if (rubric.Count is < 1 or > 10)
        {
            return (false, "Provide between 1 and 10 rubric questions.");
        }

        foreach (var (prompt, maxPoints) in rubric)
        {
            if (string.IsNullOrWhiteSpace(prompt) || prompt.Trim().Length > 500)
            {
                return (false, "Each rubric question needs a prompt of 500 characters or fewer.");
            }

            if (maxPoints is < 1 or > 100)
            {
                return (false, "Rubric question maximum points must be between 1 and 100.");
            }
        }

        var config = await _db.Set<PeerReviewConfig>()
            .Include(c => c.RubricQuestions)
            .FirstOrDefaultAsync(c => c.AssignmentId == assignmentId);

        if (config is null)
        {
            config = new PeerReviewConfig
            {
                AssignmentId = assignmentId,
                CourseId = assignment.CourseId,
            };
            _db.Set<PeerReviewConfig>().Add(config);
        }
        else if (GetPhase(config, DateTime.UtcNow) != PeerReviewPhase.Submission)
        {
            return (false, "Peer review settings and rubric are locked once the review phase opens.");
        }

        config.ReviewsPerStudent = reviewsPerStudent;
        config.IsAnonymous = isAnonymous;
        config.Strategy = strategy;
        config.InstructorWeightPercent = strategy == PeerReviewStrategy.WeightedMix ? instructorWeightPercent : 60;
        config.ReviewOpensAt = opens;
        config.ReviewClosesAt = closes.Value;
        config.UpdatedAt = DateTime.UtcNow;

        if (config.RubricQuestions.Count > 0)
        {
            _db.Set<PeerReviewRubricQuestion>().RemoveRange(config.RubricQuestions);
            config.RubricQuestions.Clear();
        }

        for (var i = 0; i < rubric.Count; i++)
        {
            config.RubricQuestions.Add(new PeerReviewRubricQuestion
            {
                SortOrder = i + 1,
                Prompt = rubric[i].Prompt.Trim(),
                MaxPoints = rubric[i].MaxPoints,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Allocation =====

    /// <summary>Runs allocation lazily when the review phase has opened and no run exists yet.</summary>
    public async Task EnsureAllocatedAsync(PeerReviewConfig config)
    {
        if (GetPhase(config, DateTime.UtcNow) == PeerReviewPhase.Submission)
        {
            return;
        }

        var hasRun = await _db.Set<PeerAllocationRun>().AnyAsync(r => r.ConfigId == config.Id);
        if (!hasRun)
        {
            await RunAllocationAsync(config, "system");

            var reviewers = await _db.Set<PeerAllocationPair>().AsNoTracking()
                .Where(p => p.ConfigId == config.Id && p.IsActive)
                .Select(p => p.ReviewerId)
                .Distinct()
                .ToListAsync();
            await _notifications.SendForManyAsync(
                EventKeys.PeerReviewOpened,
                reviewers,
                new Dictionary<string, string>(),
                $"/Courses/Assignments/PeerReview/MyReviews?assignmentId={config.AssignmentId}");
        }
    }

    /// <summary>
    /// Deterministic round-robin over ordered participants: reviewer i reviews
    /// participants[(i + offset) % n] for offset 1..k. Self-free by construction,
    /// reproducible given the same participant order, and each submission receives
    /// exactly k distinct reviews.
    /// </summary>
    public async Task<(bool Ok, string? Error)> RunAllocationAsync(PeerReviewConfig config, string actorId)
    {
        if (GetPhase(config, DateTime.UtcNow) == PeerReviewPhase.Submission)
        {
            return (false, "Allocation runs when the review phase opens.");
        }

        var submissions = await _assignments.GetSubmissionsAsync(config.AssignmentId);
        var roster = await _enrollments.GetEnrollmentsForRosterAsync(config.CourseId);
        var enrolledIds = roster.Enrollments
            .Where(e => e.RevokedAt is null)
            .Select(e => e.StudentId)
            .ToHashSet();

        var participants = submissions
            .Where(s => enrolledIds.Contains(s.StudentId))
            .OrderBy(s => s.StudentId, StringComparer.Ordinal)
            .ToList();

        var n = participants.Count;
        var k = Math.Min(config.ReviewsPerStudent, Math.Max(0, n - 1));

        var lastRunNumber = await _db.Set<PeerAllocationRun>()
            .Where(r => r.ConfigId == config.Id)
            .MaxAsync(r => (int?)r.RunNumber) ?? 0;

        var run = new PeerAllocationRun
        {
            ConfigId = config.Id,
            RunNumber = lastRunNumber + 1,
            ParticipantCount = n,
            ReviewsEach = k,
            ShortfallCount = Math.Max(0, config.ReviewsPerStudent - k) * n,
            CreatedBy = actorId,
        };
        _db.Set<PeerAllocationRun>().Add(run);

        var previousPairs = await _db.Set<PeerAllocationPair>()
            .Where(p => p.ConfigId == config.Id && p.IsActive)
            .ToListAsync();
        foreach (var pair in previousPairs)
        {
            pair.IsActive = false;
        }

        for (var offset = 1; offset <= k; offset++)
        {
            for (var i = 0; i < n; i++)
            {
                var reviewer = participants[i].StudentId;
                var revieweeSubmissionId = participants[(i + offset) % n].Id;
                _db.Set<PeerAllocationPair>().Add(new PeerAllocationPair
                {
                    Run = run,
                    ConfigId = config.Id,
                    ReviewerId = reviewer,
                    RevieweeSubmissionId = revieweeSubmissionId,
                    IsActive = true,
                });
            }
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<PeerAllocationRun?> GetLatestRunAsync(int configId)
    {
        return _db.Set<PeerAllocationRun>().AsNoTracking()
            .Where(r => r.ConfigId == configId)
            .OrderByDescending(r => r.RunNumber)
            .FirstOrDefaultAsync();
    }

    public Task<int> CountActivePairsAsync(int configId)
    {
        return _db.Set<PeerAllocationPair>().CountAsync(p => p.ConfigId == configId && p.IsActive);
    }

    public Task<int> CountAssessmentsAsync(int configId)
    {
        return _db.Set<PeerReviewAssessment>().CountAsync(a => a.ConfigId == configId);
    }

    public sealed record ReviewerPair(PeerAllocationPair Pair, int AssignmentId);

    /// <summary>Returns the active allocation pair only when it belongs to the given reviewer.</summary>
    public async Task<ReviewerPair?> GetPairForReviewerAsync(int pairId, string reviewerId)
    {
        var pair = await _db.Set<PeerAllocationPair>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pairId && p.IsActive && p.ReviewerId == reviewerId);
        if (pair is null)
        {
            return null;
        }

        var assignmentId = await _db.Set<PeerReviewConfig>().AsNoTracking()
            .Where(c => c.Id == pair.ConfigId)
            .Select(c => c.AssignmentId)
            .FirstOrDefaultAsync();
        return new ReviewerPair(pair, assignmentId);
    }

    public sealed record ReviewQueueItem(
        PeerAllocationPair Pair,
        AssignmentSubmissionView Submission,
        PeerReviewAssessment? SubmittedAssessment);

    public sealed record AssignmentSubmissionView(
        int SubmissionId,
        string Text,
        string? FileUrl,
        DateTime SubmittedAt);

    public async Task<List<ReviewQueueItem>> GetReviewerQueueAsync(int configId, string reviewerId)
    {
        var pairs = await _db.Set<PeerAllocationPair>().AsNoTracking()
            .Where(p => p.ConfigId == configId && p.ReviewerId == reviewerId && p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (pairs.Count == 0)
        {
            return new List<ReviewQueueItem>();
        }

        var submissionIds = pairs.Select(p => p.RevieweeSubmissionId).ToList();
        var submissions = await _db.Set<global::OpenLearning.Assignments.Models.AssignmentSubmission>().AsNoTracking()
            .Where(s => submissionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var assessments = await _db.Set<PeerReviewAssessment>().AsNoTracking()
            .Where(a => a.ConfigId == configId && a.AssessorId == reviewerId)
            .ToDictionaryAsync(a => a.RevieweeSubmissionId);

        var items = new List<ReviewQueueItem>();
        foreach (var pair in pairs)
        {
            if (!submissions.TryGetValue(pair.RevieweeSubmissionId, out var submission))
            {
                continue;
            }

            assessments.TryGetValue(pair.RevieweeSubmissionId, out var assessment);
            items.Add(new ReviewQueueItem(
                pair,
                new AssignmentSubmissionView(submission.Id, submission.Text, submission.FileUrl, submission.SubmittedAt),
                assessment));
        }

        return items;
    }

    // ===== Assessment submission =====

    public async Task<(bool Ok, string? Error)> SubmitAssessmentAsync(
        int pairId, string assessorId, IReadOnlyDictionary<int, (int Score, string? Comment)> answers)
    {
        var pair = await _db.Set<PeerAllocationPair>()
            .FirstOrDefaultAsync(p => p.Id == pairId && p.IsActive);
        if (pair is null || pair.ReviewerId != assessorId)
        {
            return (false, "This submission is not in your review queue.");
        }

        var config = await _db.Set<PeerReviewConfig>().AsNoTracking()
            .Include(c => c.RubricQuestions)
            .FirstAsync(c => c.Id == pair.ConfigId);

        if (GetPhase(config, DateTime.UtcNow) != PeerReviewPhase.Review)
        {
            return (false, "The review phase is not open.");
        }

        if (!await _enrollments.IsEnrolledAsync(assessorId, config.CourseId))
        {
            return (false, "You are no longer enrolled in this course.");
        }

        var existing = await _db.Set<PeerReviewAssessment>()
            .FirstOrDefaultAsync(a =>
                a.ConfigId == config.Id &&
                a.AssessorId == assessorId &&
                a.RevieweeSubmissionId == pair.RevieweeSubmissionId);
        if (existing is not null)
        {
            return (false, "You have already assessed this submission.");
        }

        var missing = config.RubricQuestions.Where(q => !answers.ContainsKey(q.Id)).ToList();
        if (missing.Count > 0)
        {
            return (false, "Score every rubric question before submitting.");
        }

        var assessment = new PeerReviewAssessment
        {
            ConfigId = config.Id,
            AssessorId = assessorId,
            RevieweeSubmissionId = pair.RevieweeSubmissionId,
        };

        foreach (var question in config.RubricQuestions.OrderBy(q => q.SortOrder))
        {
            var (score, comment) = answers[question.Id];
            if (score is < 0)
            {
                return (false, "Scores cannot be negative.");
            }

            if (score > question.MaxPoints)
            {
                return (false, $"Score for \"{question.Prompt}\" cannot exceed {question.MaxPoints} points.");
            }

            assessment.TotalScore += score;
            assessment.Answers.Add(new PeerAssessmentAnswer
            {
                QuestionId = question.Id,
                PromptSnapshot = question.Prompt,
                MaxPoints = question.MaxPoints,
                Score = score,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            });
        }

        assessment.SubmittedAt = DateTime.UtcNow;
        _db.Set<PeerReviewAssessment>().Add(assessment);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Received assessments and results =====

    public sealed record ReceivedAssessment(
        int AssessmentId,
        string? AssessorName,
        int TotalScore,
        int RubricMax,
        List<PeerAssessmentAnswer> Answers,
        DateTime SubmittedAt);

    /// <summary>
    /// Assessments received by a student, deduplicated per assessor (latest wins).
    /// Returns an empty list until results are released; assessor identity is
    /// stripped when the configuration is anonymous.
    /// </summary>
    public async Task<(List<ReceivedAssessment> Items, int RubricMax)> GetReceivedAssessmentsAsync(
        PeerReviewConfig config, string studentId)
    {
        if (config.ResultsReleasedAt is null)
        {
            return (new List<ReceivedAssessment>(), 0);
        }

        var mySubmissionIds = await _db.Set<global::OpenLearning.Assignments.Models.AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == config.AssignmentId && s.StudentId == studentId)
            .Select(s => s.Id)
            .ToListAsync();

        if (mySubmissionIds.Count == 0)
        {
            return (new List<ReceivedAssessment>(), 0);
        }

        var assessments = await _db.Set<PeerReviewAssessment>().AsNoTracking()
            .Include(a => a.Answers)
            .Where(a => a.ConfigId == config.Id && mySubmissionIds.Contains(a.RevieweeSubmissionId))
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();

        var latestByAssessor = assessments
            .GroupBy(a => a.AssessorId)
            .Select(g => g.Last())
            .ToList();

        var rubricMax = config.RubricQuestions.Sum(q => q.MaxPoints);

        Dictionary<string, string> names = new();
        if (!config.IsAnonymous && latestByAssessor.Count > 0)
        {
            var users = await _users.GetByIdsAsync(latestByAssessor.Select(a => a.AssessorId));
            names = users
                .Where(u => u is not null)
                .ToDictionary(u => u!.Id, u => u!.DisplayName);
        }

        string? ResolveName(string assessorId) =>
            config.IsAnonymous ? null : names.GetValueOrDefault(assessorId);

        var items = latestByAssessor.Select(a => new ReceivedAssessment(
            a.Id,
            ResolveName(a.AssessorId),
            a.TotalScore,
            rubricMax,
            a.Answers.OrderBy(x => x.QuestionId).ToList(),
            a.SubmittedAt)).ToList();

        return (items, rubricMax);
    }

    public async Task<int> CountParticipantsAsync(PeerReviewConfig config)
    {
        var submitters = await _db.Set<global::OpenLearning.Assignments.Models.AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == config.AssignmentId)
            .Select(s => s.StudentId)
            .Distinct()
            .ToListAsync();
        var reviewers = await _db.Set<PeerAllocationPair>().AsNoTracking()
            .Where(p => p.ConfigId == config.Id && p.IsActive)
            .Select(p => p.ReviewerId)
            .Distinct()
            .ToListAsync();
        return submitters.Union(reviewers).Count();
    }

    /// <summary>Recomputes result rows for every participant. Existing overrides are preserved.</summary>
    public async Task ComputeResultsAsync(PeerReviewConfig config)
    {
        var rubricMax = config.RubricQuestions.Sum(q => q.MaxPoints);
        var submissions = await _db.Set<global::OpenLearning.Assignments.Models.AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == config.AssignmentId)
            .ToDictionaryAsync(s => s.StudentId);

        var assessments = await _db.Set<PeerReviewAssessment>().AsNoTracking()
            .Where(a => a.ConfigId == config.Id)
            .ToListAsync();
        var peerPctByStudent = assessments
            .GroupBy(a => a.RevieweeSubmissionId)
            .ToDictionary(g => g.Key, g => g
                .GroupBy(a => a.AssessorId)
                .Select(gg => gg.OrderBy(a => a.SubmittedAt).Last())
                .Select(a => rubricMax == 0 ? 0d : (double)a.TotalScore / rubricMax * 100d)
                .DefaultIfEmpty(double.NaN)
                .Average());

        var participantIds = submissions.Keys.Union(await _db.Set<PeerAllocationPair>().AsNoTracking()
            .Where(p => p.ConfigId == config.Id && p.IsActive)
            .Select(p => p.ReviewerId)
            .Distinct()
            .ToListAsync()).ToList();

        var existing = await _db.Set<PeerReviewResult>()
            .Where(r => r.ConfigId == config.Id)
            .ToDictionaryAsync(r => r.StudentId);

        foreach (var studentId in participantIds)
        {
            double instructorPct = double.NaN;
            if (submissions.TryGetValue(studentId, out var submission) &&
                submission.GradedAt is not null &&
                submission.Score is not null)
            {
                instructorPct = (double)submission.Score.Value;
            }

            var hasInstructor = !double.IsNaN(instructorPct);

            double peerPct = double.NaN;
            if (submissions.TryGetValue(studentId, out var ownSubmission) &&
                peerPctByStudent.TryGetValue(ownSubmission.Id, out var peerAverage))
            {
                peerPct = peerAverage;
            }

            var hasPeer = !double.IsNaN(peerPct);

            int? computed;
            string basis;
            switch (config.Strategy)
            {
                case PeerReviewStrategy.InstructorOnly:
                    (computed, basis) = hasInstructor
                        ? ((int?)Math.Round(instructorPct), "instructor")
                        : (null, "pending");
                    break;
                case PeerReviewStrategy.PeerAverage:
                    (computed, basis) = hasPeer
                        ? ((int?)Math.Round(peerPct), "peer")
                        : (null, "pending");
                    break;
                default:
                    var w = config.InstructorWeightPercent / 100d;
                    if (hasInstructor && hasPeer)
                    {
                        computed = (int)Math.Round(instructorPct * w + peerPct * (1 - w));
                        basis = "instructor+peer";
                    }
                    else if (hasInstructor)
                    {
                        computed = (int)Math.Round(instructorPct);
                        basis = "instructor";
                    }
                    else if (hasPeer)
                    {
                        computed = (int)Math.Round(peerPct);
                        basis = "peer";
                    }
                    else
                    {
                        computed = null;
                        basis = "pending";
                    }

                    break;
            }

            computed = computed is null ? null : Math.Clamp(computed.Value, 0, 100);

            if (existing.TryGetValue(studentId, out var row))
            {
                row.ComputedScore = computed;
                row.Basis = basis;
                row.ComputedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Set<PeerReviewResult>().Add(new PeerReviewResult
                {
                    ConfigId = config.Id,
                    StudentId = studentId,
                    ComputedScore = computed,
                    Basis = basis,
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<(bool Ok, string? Error)> ReleaseResultsAsync(PeerReviewConfig config, string actorId)
    {
        if (GetPhase(config, DateTime.UtcNow) != PeerReviewPhase.Closed)
        {
            return (false, "Results can only be released after the review phase closes.");
        }

        if (config.ResultsReleasedAt is null)
        {
            await ComputeResultsAsync(config);

            var tracked = await _db.Set<PeerReviewConfig>().FirstAsync(c => c.Id == config.Id);
            tracked.ResultsReleasedAt = DateTime.UtcNow;
            tracked.ReleasedBy = actorId;
            await _db.SaveChangesAsync();

            var recipients = await _db.Set<PeerReviewResult>().AsNoTracking()
                .Where(r => r.ConfigId == config.Id)
                .Select(r => r.StudentId)
                .ToListAsync();
            await _notifications.SendForManyAsync(
                EventKeys.PeerReviewResultsReleased,
                recipients,
                new Dictionary<string, string>(),
                $"/Courses/Assignments/PeerReview/MyReviews?assignmentId={config.AssignmentId}");
        }

        return (true, null);
    }

    public async Task<List<PeerReviewResult>> GetInstructorResultsAsync(int configId)
    {
        return await _db.Set<PeerReviewResult>().AsNoTracking()
            .Where(r => r.ConfigId == configId)
            .OrderBy(r => r.StudentId)
            .ToListAsync();
    }

    public Task<PeerReviewResult?> GetMyResultAsync(int configId, string studentId)
    {
        return _db.Set<PeerReviewResult>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.ConfigId == configId && r.StudentId == studentId);
    }

    public async Task<(bool Ok, string? Error)> SetOverrideAsync(
        int configId, string studentId, int? overrideScore, string actorId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return (false, "A student is required.");
        }

        if (overrideScore is < 0 or > 100)
        {
            return (false, "Override score must be between 0 and 100.");
        }

        var row = await _db.Set<PeerReviewResult>()
            .FirstOrDefaultAsync(r => r.ConfigId == configId && r.StudentId == studentId);
        if (row is null)
        {
            return (false, "No result exists for this student yet.");
        }

        row.OverrideScore = overrideScore;
        row.OverrideBy = overrideScore is null ? null : actorId;
        row.OverrideAt = overrideScore is null ? null : DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
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

    /// <summary>Template event keys contributed by this module.</summary>
    public static class EventKeys
    {
        public const string PeerReviewOpened = "peer-review.opened";
        public const string PeerReviewResultsReleased = "peer-review.results-released";
    }
}
