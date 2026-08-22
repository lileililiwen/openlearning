using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Exams.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Exams.Services;

/// <summary>Input for a single integrity event during ingestion.</summary>
public sealed record EvidenceInput(long Sequence, IntegrityEventType EventType, DateTime ClientTimestamp, string? Payload);

/// <summary>Result of an evidence ingestion call.</summary>
public sealed record EvidenceIngestResult(
    bool Accepted, string? Error, long LastSequence, bool Replayed, int AcceptedCount);

/// <summary>One explainable contribution to a risk score.</summary>
public sealed record RiskContribution(string Rule, int Weight, int Count);

/// <summary>Result of risk evaluation for an attempt.</summary>
public sealed record RiskEvaluation(
    int Score, IntegrityRiskLevel Level, IReadOnlyList<RiskContribution> Contributions, int PolicyVersion);

/// <summary>
/// Exam integrity: server-authoritative signed sessions, monotonic deduplicated
/// evidence ingestion, versioned explainable risk scoring, accommodations,
/// human-only review/disposition, appeals, retention, and audited access.
/// </summary>
public class ExamIntegrityService
{
    private readonly DbContext _db;
    private readonly NotificationService _notifications;
    private readonly string _secret;

    public ExamIntegrityService(DbContext db, NotificationService notifications, IConfiguration config)
    {
        _db = db;
        _notifications = notifications;
        _secret = config["Integrity:SigningKey"] ?? "openlearning-integrity-dev-key";
    }

    // ===== Policy (versioned) =====

    public async Task<IntegrityPolicy> GetEffectivePolicyAsync(int examId)
    {
        var examPolicy = await _db.Set<IntegrityPolicy>().AsNoTracking()
            .Where(p => p.ExamId == examId && p.IsActive)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();
        if (examPolicy is not null)
        {
            return examPolicy;
        }

        return await _db.Set<IntegrityPolicy>().AsNoTracking()
                   .Where(p => p.ExamId == null && p.IsActive)
                   .OrderByDescending(p => p.Version)
                   .FirstOrDefaultAsync()
               ?? new IntegrityPolicy { ExamId = null, Version = 1, IsActive = true };
    }

    public async Task<IntegrityPolicy> CreatePolicyAsync(
        int? examId,
        int riskThreshold,
        int heartbeatGapWeight,
        int visibilityHiddenWeight,
        int tabSwitchWeight,
        int copyAttemptWeight,
        int pasteAttemptWeight,
        int connectivityLossWeight,
        int retentionDays,
        string? grantedById = null)
    {
        var version = await _db.Set<IntegrityPolicy>()
            .Where(p => p.ExamId == examId)
            .Select(p => (int?)p.Version)
            .MaxAsync() ?? 0;

        var policy = new IntegrityPolicy
        {
            ExamId = examId,
            Version = version + 1,
            IsActive = true,
            RiskThreshold = riskThreshold,
            HeartbeatGapWeight = heartbeatGapWeight,
            VisibilityHiddenWeight = visibilityHiddenWeight,
            TabSwitchWeight = tabSwitchWeight,
            CopyAttemptWeight = copyAttemptWeight,
            PasteAttemptWeight = pasteAttemptWeight,
            ConnectivityLossWeight = connectivityLossWeight,
            RetentionDays = retentionDays,
        };

        var prior = await _db.Set<IntegrityPolicy>()
            .Where(p => p.ExamId == examId && p.IsActive)
            .ToListAsync();
        foreach (var p in prior)
        {
            p.IsActive = false;
        }

        _db.Set<IntegrityPolicy>().Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    public Task<List<IntegrityPolicy>> ListPoliciesAsync(int? examId)
    {
        var query = _db.Set<IntegrityPolicy>().AsNoTracking().AsQueryable();
        if (examId.HasValue)
        {
            query = query.Where(p => p.ExamId == examId || p.ExamId == null);
        }

        return query.OrderBy(p => p.ExamId).ThenByDescending(p => p.Version).ToListAsync();
    }

    // ===== Accommodations (no diagnosis disclosure) =====

    public Task<LearnerAccommodation?> GetAccommodationAsync(int examId, string studentId)
    {
        return _db.Set<LearnerAccommodation>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExamId == examId && a.StudentId == studentId);
    }

    public async Task<LearnerAccommodation> GrantAccommodationAsync(
        int examId, string studentId, int extraMinutes, int allowedBreaks,
        int relaxedVisibilityThreshold, int relaxedCopyPasteThreshold, string? grantedById)
    {
        var existing = await _db.Set<LearnerAccommodation>()
            .FirstOrDefaultAsync(a => a.ExamId == examId && a.StudentId == studentId);
        if (existing is not null)
        {
            existing.ExtraMinutes = extraMinutes;
            existing.AllowedBreaks = allowedBreaks;
            existing.RelaxedVisibilityThreshold = relaxedVisibilityThreshold;
            existing.RelaxedCopyPasteThreshold = relaxedCopyPasteThreshold;
            existing.GrantedById = grantedById;
            existing.CreatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existing;
        }

        var accommodation = new LearnerAccommodation
        {
            ExamId = examId,
            StudentId = studentId,
            ExtraMinutes = extraMinutes,
            AllowedBreaks = allowedBreaks,
            RelaxedVisibilityThreshold = relaxedVisibilityThreshold,
            RelaxedCopyPasteThreshold = relaxedCopyPasteThreshold,
            GrantedById = grantedById,
        };
        _db.Set<LearnerAccommodation>().Add(accommodation);
        await _db.SaveChangesAsync();
        return accommodation;
    }

    public async Task<bool> RevokeAccommodationAsync(int id)
    {
        var accommodation = await _db.Set<LearnerAccommodation>().FindAsync(id);
        if (accommodation is null)
        {
            return false;
        }

        _db.Set<LearnerAccommodation>().Remove(accommodation);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== Signed sessions =====

    /// <summary>
    /// Begins (or reconnects to) a server-authoritative session for an attempt.
    /// The deadline is server time plus the exam duration and any accommodation
    /// extra time, so client clock changes cannot extend it.
    /// </summary>
    public async Task<(IntegritySession Session, string Token)> BeginSessionAsync(int attemptId, string studentId)
    {
        var attempt = await _db.Set<ExamAttempt>()
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt is null || attempt.StudentId != studentId)
        {
            throw new InvalidOperationException("Attempt not found or not owned by the caller.");
        }

        var active = await _db.Set<IntegritySession>()
            .FirstOrDefaultAsync(s => s.AttemptId == attemptId && s.Status == IntegritySessionStatus.Active);
        if (active is not null)
        {
            return (active, BuildToken(active));
        }

        var accommodation = await _db.Set<LearnerAccommodation>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExamId == attempt.ExamId && a.StudentId == studentId);
        var extraMinutes = accommodation?.ExtraMinutes ?? 0;

        var expiresAt = DateTime.UtcNow.AddMinutes(attempt.Exam!.DurationMinutes + extraMinutes);
        var session = new IntegritySession
        {
            AttemptId = attemptId,
            Nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
            ExpiresAt = expiresAt,
        };

        if (accommodation is not null && accommodation.AttemptId != attemptId)
        {
            accommodation.AttemptId = attemptId;
        }

        _db.Set<IntegritySession>().Add(session);
        await _db.SaveChangesAsync();

        // Sign after the row has a real Id so the token binds the persisted session.
        session.Signature = Sign(session.Id, session.AttemptId, session.Nonce, session.ExpiresAt);
        await _db.SaveChangesAsync();
        return (session, BuildToken(session));
    }

    public bool ValidateToken(int sessionId, string token)
    {
        var parts = token.Split('.', 5);
        if (parts.Length != 5)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var tokenSessionId) || tokenSessionId != sessionId)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var attemptId) || parts[2].Length == 0)
        {
            return false;
        }

        var expiresAt = DateTime.FromBinary(long.Parse(parts[3], CultureInfo.InvariantCulture));
        var expected = Sign(tokenSessionId, attemptId, parts[2], expiresAt);
        return expected == parts[4];
    }

    private static string BuildToken(IntegritySession session)
    {
        return $"{session.Id}.{session.AttemptId}.{session.Nonce}.{session.ExpiresAt.ToBinary()}.{session.Signature}";
    }

    private string Sign(int sessionId, int attemptId, string nonce, DateTime expiresAt)
    {
        var raw = $"{sessionId}:{attemptId}:{nonce}:{expiresAt.ToBinary()}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }

    // ===== Evidence ingestion (monotonic, deduplicated, reconnect-safe) =====

    public async Task<EvidenceIngestResult> IngestAsync(
        int sessionId, string token, string batchId, IReadOnlyList<EvidenceInput> events)
    {
        var session = await _db.Set<IntegritySession>()
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return new EvidenceIngestResult(false, "Session not found.", 0, false, 0);
        }

        if (session.Status != IntegritySessionStatus.Active)
        {
            return new EvidenceIngestResult(false, "Session is closed.", session.LastSequence, false, 0);
        }

        if (!ValidateToken(sessionId, token))
        {
            return new EvidenceIngestResult(false, "Invalid session token.", session.LastSequence, false, 0);
        }

        if (DateTime.UtcNow > session.ExpiresAt)
        {
            session.Status = IntegritySessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return new EvidenceIngestResult(false, "Session expired.", session.LastSequence, false, 0);
        }

        // Replayed batch: return the prior acknowledgement without double counting.
        var priorBatch = await _db.Set<IntegrityEvidence>().AsNoTracking()
            .AnyAsync(e => e.SessionId == sessionId && e.BatchId == batchId && e.Accepted);
        if (priorBatch)
        {
            return new EvidenceIngestResult(true, null, session.LastSequence, true, 0);
        }

        var accepted = new List<IntegrityEvidence>();
        foreach (var ev in events.OrderBy(e => e.Sequence))
        {
            // Out-of-order or replayed sequence is ignored, never counted twice.
            if (ev.Sequence <= session.LastSequence)
            {
                continue;
            }

            accepted.Add(new IntegrityEvidence
            {
                SessionId = sessionId,
                AttemptId = session.AttemptId,
                Sequence = ev.Sequence,
                BatchId = batchId,
                EventType = ev.EventType,
                ClientTimestamp = ev.ClientTimestamp,
                Payload = ev.Payload,
                Accepted = true,
            });
            session.LastSequence = ev.Sequence;
            session.LastEventAt = DateTime.UtcNow;
        }

        if (accepted.Count > 0)
        {
            _db.Set<IntegrityEvidence>().AddRange(accepted);
            await _db.SaveChangesAsync();
        }

        return new EvidenceIngestResult(true, null, session.LastSequence, false, accepted.Count);
    }

    // ===== Explainable, versioned risk evaluation =====

    public async Task<RiskEvaluation> EvaluateAsync(int attemptId)
    {
        var policy = await GetEffectivePolicyForAttemptAsync(attemptId);
        var evidence = await _db.Set<IntegrityEvidence>().AsNoTracking()
            .Where(e => e.AttemptId == attemptId && e.Accepted)
            .ToListAsync();

        var accommodation = await _db.Set<LearnerAccommodation>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        var relaxedVisibility = accommodation?.RelaxedVisibilityThreshold ?? 0;
        var relaxedCopyPaste = accommodation?.RelaxedCopyPasteThreshold ?? 0;

        var contributions = new List<RiskContribution>();
        int score = 0;

        void Add(IntegrityEventType type, int weight, int relaxedThreshold = 0)
        {
            var count = evidence.Count(e => e.EventType == type);
            var effective = Math.Max(0, count - relaxedThreshold);
            if (effective <= 0)
            {
                return;
            }

            var rule = type.ToString();
            var added = effective * weight;
            contributions.Add(new RiskContribution(rule, weight, effective));
            score += added;
        }

        Add(IntegrityEventType.Heartbeat, policy.HeartbeatGapWeight);
        Add(IntegrityEventType.VisibilityHidden, policy.VisibilityHiddenWeight, relaxedVisibility);
        Add(IntegrityEventType.TabSwitch, policy.TabSwitchWeight);
        Add(IntegrityEventType.CopyAttempt, policy.CopyAttemptWeight, relaxedCopyPaste);
        Add(IntegrityEventType.PasteAttempt, policy.PasteAttemptWeight, relaxedCopyPaste);
        Add(IntegrityEventType.ConnectivityLost, policy.ConnectivityLossWeight);

        IntegrityRiskLevel level;
        if (score >= policy.RiskThreshold)
        {
            level = IntegrityRiskLevel.High;
        }
        else if (score >= policy.RiskThreshold / 2)
        {
            level = IntegrityRiskLevel.Medium;
        }
        else if (score > 0)
        {
            level = IntegrityRiskLevel.Low;
        }
        else
        {
            level = IntegrityRiskLevel.None;
        }

        return new RiskEvaluation(score, level, contributions, policy.Version);
    }

    /// <summary>
    /// Evaluates and, when the threshold is crossed, queues an incident. High
    /// risk never changes a grade; only a human reviewer can disposition.
    /// </summary>
    public async Task<IntegrityIncident?> EvaluateAndQueueAsync(int attemptId)
    {
        var evaluation = await EvaluateAsync(attemptId);
        if (evaluation.Level != IntegrityRiskLevel.High)
        {
            return null;
        }

        var attempt = await _db.Set<ExamAttempt>()
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
        if (attempt is null)
        {
            return null;
        }

        var existing = await _db.Set<IntegrityIncident>().AsNoTracking()
            .AnyAsync(i => i.AttemptId == attemptId && i.Status != IntegrityIncidentStatus.Closed);
        if (existing)
        {
            return null;
        }

        var incident = new IntegrityIncident
        {
            AttemptId = attemptId,
            ExamId = attempt.ExamId,
            CourseId = attempt.Exam!.CourseId,
            StudentId = attempt.StudentId,
            RiskLevel = evaluation.Level,
            RiskScore = evaluation.Score,
            ContributingRules = System.Text.Json.JsonSerializer.Serialize(
                evaluation.Contributions.Select(c => new { c.Rule, c.Weight, c.Count })),
            PolicyVersion = evaluation.PolicyVersion,
            Status = IntegrityIncidentStatus.Open,
        };
        _db.Set<IntegrityIncident>().Add(incident);
        await _db.SaveChangesAsync();
        return incident;
    }

    private async Task<IntegrityPolicy> GetEffectivePolicyForAttemptAsync(int attemptId)
    {
        var examId = await _db.Set<ExamAttempt>().AsNoTracking()
            .Where(a => a.Id == attemptId)
            .Select(a => a.ExamId)
            .FirstAsync();
        return await GetEffectivePolicyAsync(examId);
    }

    // ===== Reviewer scope + audited access =====

    /// <summary>True only if the reviewer owns the course the incident belongs to.</summary>
    public async Task<bool> CanReviewAsync(int incidentId, string reviewerId)
    {
        return await _db.Set<IntegrityIncident>().AsNoTracking()
            .AnyAsync(i => i.Id == incidentId && i.CourseId != 0
                && _db.Set<Course>().AsNoTracking()
                    .Any(c => c.Id == i.CourseId && c.InstructorId == reviewerId));
    }

    public async Task<IntegrityIncident?> GetIncidentForReviewAsync(int incidentId, string reviewerId)
    {
        if (!await CanReviewAsync(incidentId, reviewerId))
        {
            return null;
        }

        await AuditAsync(incidentId, null, reviewerId, IntegrityAccessAction.ViewIncident);
        return await _db.Set<IntegrityIncident>().AsNoTracking()
            .Include(i => i.Dispositions)
            .Include(i => i.Appeals)
            .FirstOrDefaultAsync(i => i.Id == incidentId);
    }

    public async Task<List<IntegrityEvidence>> GetEvidenceForReviewAsync(int incidentId, string reviewerId)
    {
        if (!await CanReviewAsync(incidentId, reviewerId))
        {
            return new List<IntegrityEvidence>();
        }

        var attemptId = await _db.Set<IntegrityIncident>().AsNoTracking()
            .Where(i => i.Id == incidentId)
            .Select(i => i.AttemptId)
            .FirstAsync();

        await AuditAsync(incidentId, null, reviewerId, IntegrityAccessAction.ViewEvidence);
        return await _db.Set<IntegrityEvidence>().AsNoTracking()
            .Where(e => e.AttemptId == attemptId && e.Accepted)
            .OrderBy(e => e.Sequence)
            .ToListAsync();
    }

    public async Task<List<IntegrityIncident>> ListIncidentsForReviewerAsync(string reviewerId)
    {
        var courseIds = await _db.Set<Course>().AsNoTracking()
            .Where(c => c.InstructorId == reviewerId)
            .Select(c => c.Id)
            .ToListAsync();
        return await _db.Set<IntegrityIncident>().AsNoTracking()
            .Where(i => courseIds.Contains(i.CourseId))
            .OrderByDescending(i => i.DetectedAt)
            .ToListAsync();
    }

    private async Task AuditAsync(int? incidentId, int? sessionId, string reviewerId, IntegrityAccessAction action)
    {
        _db.Set<IntegrityAccessLog>().Add(new IntegrityAccessLog
        {
            IncidentId = incidentId,
            SessionId = sessionId,
            ReviewerId = reviewerId,
            Action = action,
        });
        await _db.SaveChangesAsync();
    }

    // ===== Disposition (human-only, audited, notifies on adverse) =====

    public async Task<(IntegrityDisposition? Disposition, string? Error)> RecordDispositionAsync(
        int incidentId, string reviewerId, IntegrityDispositionOutcome outcome, string? notes)
    {
        if (!await CanReviewAsync(incidentId, reviewerId))
        {
            return (null, "You are not authorized to review this incident.");
        }

        var incident = await _db.Set<IntegrityIncident>()
            .Include(i => i.Attempt)
            .FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident is null)
        {
            return (null, "Incident not found.");
        }

        var disposition = new IntegrityDisposition
        {
            IncidentId = incidentId,
            ReviewerId = reviewerId,
            Outcome = outcome,
            Notes = notes,
            AuditedAt = DateTime.UtcNow,
        };
        _db.Set<IntegrityDisposition>().Add(disposition);
        incident.Status = IntegrityIncidentStatus.Dispositioned;

        if (outcome != IntegrityDispositionOutcome.NoAction)
        {
            await _notifications.SendAsync(
                NotificationService.EventKeys.IntegrityDisposition,
                incident.StudentId,
                new Dictionary<string, string>
                {
                    ["exam"] = incident.Attempt?.Exam?.Title ?? "exam",
                    ["outcome"] = outcome.ToString(),
                });
        }

        await AuditAsync(incidentId, null, reviewerId, IntegrityAccessAction.RecordDisposition);
        await _db.SaveChangesAsync();
        return (disposition, null);
    }

    // ===== Appeals =====

    public async Task<(IntegrityAppeal? Appeal, string? Error)> SubmitAppealAsync(
        int incidentId, string studentId, string reason)
    {
        var incident = await _db.Set<IntegrityIncident>().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident is null)
        {
            return (null, "Incident not found.");
        }

        if (incident.StudentId != studentId)
        {
            return (null, "You may only appeal your own incident.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return (null, "Please provide a reason for the appeal.");
        }

        var appeal = new IntegrityAppeal
        {
            IncidentId = incidentId,
            StudentId = studentId,
            Reason = reason.Trim(),
        };
        _db.Set<IntegrityAppeal>().Add(appeal);
        incident.Status = IntegrityIncidentStatus.Appealed;
        await _db.SaveChangesAsync();
        return (appeal, null);
    }

    public async Task<(IntegrityAppeal? Appeal, string? Error)> DecideAppealAsync(
        int appealId, string reviewerId, IntegrityAppealStatus status, string? notes)
    {
        var appeal = await _db.Set<IntegrityAppeal>()
            .Include(a => a.Incident)
            .FirstOrDefaultAsync(a => a.Id == appealId);
        if (appeal is null)
        {
            return (null, "Appeal not found.");
        }

        if (!await CanReviewAsync(appeal.IncidentId, reviewerId))
        {
            return (null, "You are not authorized to review this incident.");
        }

        appeal.Status = status;
        appeal.ReviewerId = reviewerId;
        appeal.ReviewerNotes = notes;
        appeal.DecidedAt = DateTime.UtcNow;
        appeal.Incident!.Status = status == IntegrityAppealStatus.Overturned
            ? IntegrityIncidentStatus.Closed
            : IntegrityIncidentStatus.Dispositioned;

        await AuditAsync(appeal.IncidentId, null, reviewerId, IntegrityAccessAction.DecideAppeal);
        await _db.SaveChangesAsync();
        return (appeal, null);
    }

    public Task<List<IntegrityAppeal>> ListAppealsForStudentAsync(string studentId)
    {
        return _db.Set<IntegrityAppeal>().AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    // ===== Retention =====

    /// <summary>Purges accepted evidence older than the applicable policy retention.</summary>
    public async Task<int> PurgeExpiredEvidenceAsync()
    {
        var cutoff = DateTime.UtcNow;
        var toDelete = new List<IntegrityEvidence>();
        var all = await _db.Set<IntegrityEvidence>()
            .Include(e => e.Session)
            .ToListAsync();
        foreach (var e in all)
        {
            var retention = e.Session?.ExpiresAt != null
                ? (await PolicyRetentionForAttemptAsync(e.AttemptId))
                : 90;
            var expireAt = e.ReceivedAt.AddDays(retention);
            if (expireAt < cutoff)
            {
                toDelete.Add(e);
            }
        }

        if (toDelete.Count == 0)
        {
            return 0;
        }

        _db.Set<IntegrityEvidence>().RemoveRange(toDelete);
        await _db.SaveChangesAsync();
        return toDelete.Count;
    }

    private async Task<int> PolicyRetentionForAttemptAsync(int attemptId)
    {
        var policy = await GetEffectivePolicyForAttemptAsync(attemptId);
        return policy.RetentionDays;
    }
}
