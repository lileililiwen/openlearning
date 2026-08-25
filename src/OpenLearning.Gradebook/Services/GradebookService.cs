using Microsoft.EntityFrameworkCore;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Gradebook.Models;

namespace OpenLearning.Gradebook.Services;

/// <summary>
/// Weighted per-course gradebook over assignment, quiz, and exam scores.
/// Aggregates are computed from graded items only; excusals remove weight;
/// overrides shadow the source score. Source-of-record grades stay in their
/// owning modules.
/// </summary>
public class GradebookService
{
    private readonly DbContext _db;
    private readonly AssignmentService _assignments;
    private readonly EnrollmentService _enrollments;
    private readonly UserService _users;

    public GradebookService(
        DbContext db,
        AssignmentService assignments,
        EnrollmentService enrollments,
        UserService users)
    {
        _db = db;
        _assignments = assignments;
        _enrollments = enrollments;
        _users = users;
    }

    // ===== Configuration =====

    public async Task<GradebookConfig?> GetConfigAsync(int courseId)
    {
        return await _db.Set<GradebookConfig>().AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CourseId == courseId);
    }

    public async Task<GradebookConfig> GetOrCreateConfigAsync(int courseId)
    {
        var config = await _db.Set<GradebookConfig>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CourseId == courseId);
        if (config is not null)
        {
            return config;
        }

        config = new GradebookConfig { CourseId = courseId };
        _db.Set<GradebookConfig>().Add(config);
        await _db.SaveChangesAsync();
        return config;
    }

    public Task<int> GetWeightTotalAsync(int configId)
    {
        return _db.Set<GradebookItem>().AsNoTracking()
            .Where(i => i.ConfigId == configId)
            .SumAsync(i => (int?)i.Weight)
            .ContinueWith(t => t.Result ?? 0);
    }

    public async Task<(bool Ok, string? Error)> AddItemAsync(
        int courseId, GradebookItemKind kind, int sourceId, int weight, string requesterId, bool isAdmin)
    {
        var config = await GetOrCreateConfigAsync(courseId);
        if (weight is < 1 or > 100)
        {
            return (false, "Weight must be between 1 and 100 percent.");
        }

        if (!await SourceBelongsToCourseAsync(kind, sourceId, courseId))
        {
            return (false, "The selected activity does not belong to this course.");
        }

        var exists = await _db.Set<GradebookItem>().AsNoTracking()
            .AnyAsync(i => i.ConfigId == config.Id && i.Kind == kind && i.SourceId == sourceId);
        if (exists)
        {
            return (false, "That activity is already in the gradebook.");
        }

        var total = await GetWeightTotalAsync(config.Id);
        if (total + weight > 100)
        {
            return (false, $"Weights would total {total + weight} percent; the maximum is 100.");
        }

        var maxOrder = config.Items.Count == 0 ? 0 : config.Items.Max(i => i.SortOrder);
        _db.Set<GradebookItem>().Add(new GradebookItem
        {
            ConfigId = config.Id,
            Kind = kind,
            SourceId = sourceId,
            Weight = weight,
            SortOrder = maxOrder + 1,
        });
        config.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RemoveItemAsync(int courseId, int itemId, string requesterId, bool isAdmin)
    {
        var item = await _db.Set<GradebookItem>().AsNoTracking()
            .Include(i => i.Config)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
        {
            return (false, "Item not found.");
        }

        if (item.Config!.CourseId != courseId)
        {
            return (false, "Item does not belong to this course.");
        }

        _db.Set<GradebookAdjustment>().RemoveRange(
            _db.Set<GradebookAdjustment>().Where(a => a.ItemId == itemId));
        _db.Set<GradebookItem>().Remove(item);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task<bool> SourceBelongsToCourseAsync(GradebookItemKind kind, int sourceId, int courseId)
    {
        return kind switch
        {
            GradebookItemKind.Assignment => await _db.Set<global::OpenLearning.Assignments.Models.Assignment>().AsNoTracking()
                .AnyAsync(a => a.Id == sourceId && a.CourseId == courseId),
            GradebookItemKind.Quiz => await _db.Set<global::OpenLearning.Assessments.Models.Quiz>().AsNoTracking()
                .AnyAsync(q => q.Id == sourceId && q.CourseId == courseId),
            GradebookItemKind.Exam => await _db.Set<global::OpenLearning.Exams.Models.Exam>().AsNoTracking()
                .AnyAsync(e => e.Id == sourceId && e.CourseId == courseId),
            _ => false,
        };
    }

    /// <summary>Candidate activities available to be added to the gradebook.</summary>
    public async Task<(List<global::OpenLearning.Assignments.Models.Assignment> Assignments,
                       List<global::OpenLearning.Assessments.Models.Quiz> Quizzes,
                       List<global::OpenLearning.Exams.Models.Exam> Exams)>
        GetCandidatesAsync(int courseId)
    {
        var assignments = await _assignments.GetForCourseAsync(courseId);
        var quizzes = await _db.Set<global::OpenLearning.Assessments.Models.Quiz>().AsNoTracking()
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
        var exams = await _db.Set<global::OpenLearning.Exams.Models.Exam>().AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Id)
            .ToListAsync();
        return (assignments, quizzes, exams);
    }

    // ===== Overrides and excusals =====

    public async Task<(bool Ok, string? Error)> SetOverrideAsync(
        int itemId, string studentId, int? overrideScore, string actorId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return (false, "A student is required.");
        }

        if (overrideScore is < 0 or > 100)
        {
            return (false, "Override score must be between 0 and 100.");
        }

        var item = await _db.Set<GradebookItem>().AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
        {
            return (false, "Item not found.");
        }

        var existing = await _db.Set<GradebookAdjustment>()
            .FirstOrDefaultAsync(a => a.ItemId == itemId && a.StudentId == studentId);

        if (overrideScore is null && (existing is null || existing.IsExcusal))
        {
            return (true, null);
        }

        if (existing is null || existing.IsExcusal)
        {
            if (existing is not null)
            {
                _db.Set<GradebookAdjustment>().Remove(existing);
            }
            _db.Set<GradebookAdjustment>().Add(new GradebookAdjustment
            {
                ItemId = itemId,
                StudentId = studentId,
                IsExcusal = false,
                OverrideScore = overrideScore,
                CreatedBy = actorId,
            });
        }
        else
        {
            existing.IsExcusal = false;
            existing.OverrideScore = overrideScore;
            existing.CreatedBy = actorId;
            existing.CreatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetExcusalAsync(
        int itemId, string studentId, bool excused, string? reason, string actorId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return (false, "A student is required.");
        }

        var existing = await _db.Set<GradebookAdjustment>()
            .FirstOrDefaultAsync(a => a.ItemId == itemId && a.StudentId == studentId);

        if (!excused)
        {
            if (existing is not null && existing.IsExcusal)
            {
                _db.Set<GradebookAdjustment>().Remove(existing);
                await _db.SaveChangesAsync();
            }
            return (true, null);
        }

        if (existing is null || !existing.IsExcusal)
        {
            if (existing is not null)
            {
                _db.Set<GradebookAdjustment>().Remove(existing);
            }
            _db.Set<GradebookAdjustment>().Add(new GradebookAdjustment
            {
                ItemId = itemId,
                StudentId = studentId,
                IsExcusal = true,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedBy = actorId,
            });
        }
        else
        {
            existing.Reason = string.IsNullOrWhiteSpace(reason) ? existing.Reason : reason.Trim();
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Computation =====

    public sealed record StudentItemScore(double? Percent, bool Excused, bool Overridden);

    public sealed record StudentAggregate(string StudentId, int? Aggregate, Dictionary<int, StudentItemScore> ItemScores);

    /// <summary>
    /// Computes per-student per-item percentages and the weight-normalized
    /// aggregate from graded items only. Ungraded items count as missing, not zero.
    /// </summary>
    public async Task<List<StudentAggregate>> ComputeAsync(GradebookConfig config)
    {
        var roster = await _enrollments.GetEnrollmentsForRosterAsync(config.CourseId);
        var studentIds = roster.Enrollments
            .Where(e => e.RevokedAt is null)
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        var items = config.Items.OrderBy(i => i.SortOrder).ToList();
        if (items.Count == 0 || studentIds.Count == 0)
        {
            return studentIds.Select(s => new StudentAggregate(s, null, new Dictionary<int, StudentItemScore>())).ToList();
        }

        var assignmentIds = items.Where(i => i.Kind == GradebookItemKind.Assignment).Select(i => i.SourceId).ToList();
        var quizIds = items.Where(i => i.Kind == GradebookItemKind.Quiz).Select(i => i.SourceId).ToList();
        var examIds = items.Where(i => i.Kind == GradebookItemKind.Exam).Select(i => i.SourceId).ToList();

        var assignmentScores = await _db.Set<global::OpenLearning.Assignments.Models.AssignmentSubmission>().AsNoTracking()
            .Where(s => s.StudentId != null &&
                        assignmentIds.Contains(s.AssignmentId) && s.GradedAt != null && s.Score != null)
            .Select(s => new { s.AssignmentId, s.StudentId, s.Score })
            .ToListAsync();

        var quizScores = await _db.Set<global::OpenLearning.Assessments.Models.QuizAttempt>().AsNoTracking()
            .Where(a => quizIds.Contains(a.QuizId) && a.MaxScore > 0)
            .Select(a => new { a.QuizId, a.StudentId, a.Score, a.MaxScore })
            .ToListAsync();

        var examScores = await _db.Set<global::OpenLearning.Exams.Models.ExamAttempt>().AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.SubmittedAt != null)
            .Select(a => new { a.ExamId, a.StudentId, a.Percent })
            .ToListAsync();

        var adjustments = await _db.Set<GradebookAdjustment>().AsNoTracking()
            .Where(a => items.Select(i => i.Id).Contains(a.ItemId))
            .ToDictionaryAsync(a => (a.ItemId, a.StudentId));

        var result = new List<StudentAggregate>();
        foreach (var studentId in studentIds)
        {
            double numerator = 0;
            double denominator = 0;
            var itemScores = new Dictionary<int, StudentItemScore>();

            foreach (var item in items)
            {
                adjustments.TryGetValue((item.Id, studentId), out var adjustment);
                var excused = adjustment is { IsExcusal: true };

                double? percent = null;
                if (!excused)
                {
                    switch (item.Kind)
                    {
                        case GradebookItemKind.Assignment:
                            percent = assignmentScores
                                .Where(s => s.AssignmentId == item.SourceId && s.StudentId == studentId)
                                .Select(s => (double?)s.Score!.Value)
                                .FirstOrDefault();
                            break;
                        case GradebookItemKind.Quiz:
                            var attempts = quizScores
                                .Where(a => a.QuizId == item.SourceId && a.StudentId == studentId)
                                .Select(a => (double)a.Score * 100d / a.MaxScore)
                                .ToList();
                            percent = attempts.Count > 0 ? attempts.Max() : null;
                            break;
                        case GradebookItemKind.Exam:
                            var examPercents = examScores
                                .Where(a => a.ExamId == item.SourceId && a.StudentId == studentId)
                                .Select(a => (double)a.Percent)
                                .ToList();
                            percent = examPercents.Count > 0 ? examPercents.Max() : null;
                            break;
                    }

                    if (adjustment is { OverrideScore: not null })
                    {
                        percent = adjustment.OverrideScore.Value;
                    }
                }

                itemScores[item.Id] = new StudentItemScore(percent, excused, adjustment is { OverrideScore: not null });

                if (excused || percent is null)
                {
                    continue;
                }

                numerator += percent.Value * item.Weight;
                denominator += item.Weight;
            }

            var aggregate = denominator > 0 ? (int)Math.Round(numerator / denominator) : (int?)null;
            result.Add(new StudentAggregate(studentId, aggregate, itemScores));
        }

        return result;
    }

    // ===== Publication =====

    public async Task<(bool Ok, string? Error)> PublishAsync(GradebookConfig config, string actorId)
    {
        var total = await GetWeightTotalAsync(config.Id);
        if (total != 100)
        {
            return (false, $"Active weights total {total} percent; they must total exactly 100 before publication.");
        }

        var computed = await ComputeAsync(config);

        var tracked = await _db.Set<GradebookConfig>().FirstAsync(c => c.Id == config.Id);
        tracked.IsPublished = true;
        tracked.PublishedAt = DateTime.UtcNow;
        tracked.PublishedBy = actorId;

        var previousSnapshots = _db.Set<GradebookSnapshot>().Where(s => s.ConfigId == config.Id);
        _db.Set<GradebookSnapshot>().RemoveRange(previousSnapshots);

        foreach (var row in computed)
        {
            var basis = string.Join("; ", row.ItemScores
                .OrderBy(kvp => kvp.Key)
                .Select(kvp =>
                {
                    var item = config.Items.First(i => i.Id == kvp.Key);
                    var prefix = item.Kind.ToString().ToLowerInvariant() + ":" + item.SourceId;
                    if (kvp.Value.Excused)
                    {
                        return $"excused:{prefix}";
                    }

                    return kvp.Value.Percent is null
                        ? $"{prefix}=ungraded"
                        : $"{prefix}={Math.Round(kvp.Value.Percent.Value)}";
                }));

            _db.Set<GradebookSnapshot>().Add(new GradebookSnapshot
            {
                ConfigId = config.Id,
                StudentId = row.StudentId,
                Aggregate = row.Aggregate,
                BasisJson = basis,
                PublishedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UnpublishAsync(GradebookConfig config, string actorId)
    {
        var tracked = await _db.Set<GradebookConfig>().FirstAsync(c => c.Id == config.Id);
        tracked.IsPublished = false;
        tracked.PublishedAt = null;
        tracked.PublishedBy = null;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<GradebookSnapshot?> GetSnapshotAsync(int configId, string studentId)
    {
        return await _db.Set<GradebookSnapshot>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.ConfigId == configId && s.StudentId == studentId);
    }

    public async Task<Dictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> userIds)
    {
        var users = await _users.GetByIdsAsync(userIds.Distinct());
        return users
            .Where(u => u is not null)
            .ToDictionary(u => u!.Id, u => u!.DisplayName);
    }
}
