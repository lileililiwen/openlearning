using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Analytics.Models;

namespace OpenLearning.Analytics.Services;

/// <summary>
/// Produces daily course/cohort/assessment/workload facts from learning events.
/// Each run is atomic: facts are tagged with a refresh-run id and only become
/// visible once the run is marked succeeded, so partial runs are never served.
/// </summary>
public class AnalyticsAggregateService
{
    private readonly DbContext _db;

    public AnalyticsAggregateService(DbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Runs an atomic daily refresh for the given UTC day. Returns the refresh
    /// run id on success, or null with an error message on failure.
    /// </summary>
    public async Task<(long? RunId, string? Error)> RefreshDailyAsync(DateOnly day)
    {
        var run = new RefreshRun
        {
            Scope = "daily",
            AggregateDate = day,
            Status = RefreshRunStatus.Running,
            StartedAt = DateTime.UtcNow,
        };
        _db.Set<RefreshRun>().Add(run);
        await _db.SaveChangesAsync();

        try
        {
            var events = await _db.Set<LearningEvent>().AsNoTracking()
                .Where(e => e.OccurredAt >= day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                            && e.OccurredAt < day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1))
                .ToListAsync();

            await WriteFunnelAsync(run.Id, day, events);
            await WriteEngagementAsync(run.Id, day, events);
            await WriteCohortRetentionAsync(run.Id, events);
            await WriteAssessmentAsync(run.Id, day, events);
            await WriteWorkloadAsync(run.Id, day, events);

            run.Status = RefreshRunStatus.Succeeded;
            run.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (run.Id, null);
        }
        catch (Exception ex)
        {
            run.Status = RefreshRunStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.Error = ex.Message;
            await _db.SaveChangesAsync();
            return (null, ex.Message);
        }
    }

    /// <summary>Id of the latest succeeded refresh run, or null if none.</summary>
    public async Task<long?> GetLatestSucceededRunIdAsync()
    {
        return await _db.Set<RefreshRun>().AsNoTracking()
            .Where(r => r.Status == RefreshRunStatus.Succeeded)
            .OrderByDescending(r => r.CompletedAt)
            .Select(r => (long?)r.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>Timestamp of the latest succeeded refresh, for freshness indicators.</summary>
    public async Task<DateTime?> GetLastSuccessfulRefreshAsync()
    {
        return await _db.Set<RefreshRun>().AsNoTracking()
            .Where(r => r.Status == RefreshRunStatus.Succeeded)
            .OrderByDescending(r => r.CompletedAt)
            .Select(r => r.CompletedAt)
            .FirstOrDefaultAsync();
    }

    private async Task WriteFunnelAsync(long runId, DateOnly day, List<LearningEvent> events)
    {
        var courseIds = events.Select(e => e.CourseId).Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList();
        foreach (var courseId in courseIds)
        {
            var courseEvents = events.Where(e => e.CourseId == courseId).ToList();
            var eligible = DistinctActors(courseEvents, LearningEventSchema.CourseEnrolled);
            var started = DistinctActors(courseEvents, LearningEventSchema.CourseStarted);
            var completed = DistinctActors(courseEvents, LearningEventSchema.CourseCompleted);
            _db.Set<CourseFunnelAggregate>().Add(new CourseFunnelAggregate
            {
                RefreshRunId = runId,
                CourseId = courseId,
                Date = day,
                Eligible = eligible.Count,
                Enrolled = eligible.Count,
                Started = started.Count,
                Completed = completed.Count,
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task WriteEngagementAsync(long runId, DateOnly day, List<LearningEvent> events)
    {
        var courseIds = events.Select(e => e.CourseId).Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList();
        foreach (var courseId in courseIds)
        {
            var courseEvents = events.Where(e => e.CourseId == courseId).ToList();
            var activeLearners = courseEvents.Select(e => e.ActorKey).Distinct().Count();
            var activeSeconds = courseEvents
                .Where(e => e.EventType == LearningEventSchema.SessionActive)
                .Sum(e => (long)(GetIntProperty(e, "seconds") ?? 0));
            _db.Set<EngagementAggregate>().Add(new EngagementAggregate
            {
                RefreshRunId = runId,
                CourseId = courseId,
                Date = day,
                ActiveLearners = activeLearners,
                ActiveSeconds = activeSeconds,
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task WriteCohortRetentionAsync(long runId, List<LearningEvent> events)
    {
        var cohortKeys = events
            .Where(e => e.ClassGroupId.HasValue && e.CourseId.HasValue)
            .Select(e => (e.CourseId!.Value, e.ClassGroupId!.Value))
            .Distinct()
            .ToList();
        foreach (var (courseId, classGroupId) in cohortKeys)
        {
            var cohortEvents = events.Where(e => e.CourseId == courseId && e.ClassGroupId == classGroupId).ToList();
            var retained = cohortEvents.Select(e => e.ActorKey).Distinct().Count();
            _db.Set<CohortRetentionAggregate>().Add(new CohortRetentionAggregate
            {
                RefreshRunId = runId,
                CourseId = courseId,
                ClassGroupId = classGroupId,
                PeriodIndex = 0,
                Retained = retained,
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task WriteAssessmentAsync(long runId, DateOnly day, List<LearningEvent> events)
    {
        var assessmentIds = events.Select(e => e.AssessmentId).Where(a => a.HasValue).Select(a => a!.Value).Distinct().ToList();
        foreach (var assessmentId in assessmentIds)
        {
            var assessmentEvents = events.Where(e => e.AssessmentId == assessmentId).ToList();
            var attempts = assessmentEvents.Count(e => e.EventType == LearningEventSchema.AssessmentAttempted);
            var completions = assessmentEvents.Count(e => e.EventType == LearningEventSchema.AssessmentCompleted);
            var scores = assessmentEvents
                .Where(e => e.EventType == LearningEventSchema.AssessmentCompleted)
                .Select(e => GetDoubleProperty(e, "score"))
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
            var passed = assessmentEvents
                .Where(e => e.EventType == LearningEventSchema.AssessmentCompleted)
                .Count(e => GetBoolProperty(e, "passed") == true);
            var courseId = assessmentEvents.Select(e => e.CourseId).FirstOrDefault(c => c.HasValue) ?? 0;
            _db.Set<AssessmentAggregate>().Add(new AssessmentAggregate
            {
                RefreshRunId = runId,
                AssessmentId = assessmentId,
                CourseId = courseId,
                Date = day,
                Attempts = attempts,
                Completions = completions,
                AverageScore = scores.Count == 0 ? 0 : scores.Average(),
                PassRate = completions == 0 ? 0 : (double)passed / completions,
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task WriteWorkloadAsync(long runId, DateOnly day, List<LearningEvent> events)
    {
        var courseIds = events.Select(e => e.CourseId).Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList();
        foreach (var courseId in courseIds)
        {
            var courseEvents = events.Where(e => e.CourseId == courseId).ToList();
            var teachingHours = courseEvents
                .Where(e => e.EventType == LearningEventSchema.LiveAttended)
                .Sum(e => GetDoubleProperty(e, "hours") ?? 0);
            var gradingWorkload = courseEvents.Count(e => e.EventType == LearningEventSchema.AssessmentCompleted);
            _db.Set<WorkloadAggregate>().Add(new WorkloadAggregate
            {
                RefreshRunId = runId,
                CourseId = courseId,
                Date = day,
                TeachingHours = teachingHours,
                GradingWorkload = gradingWorkload,
            });
        }

        await _db.SaveChangesAsync();
    }

    private static HashSet<string> DistinctActors(IEnumerable<LearningEvent> events, string eventType)
    {
        return events.Where(e => e.EventType == eventType).Select(e => e.ActorKey).ToHashSet();
    }

    private static int? GetIntProperty(LearningEvent e, string name)
    {
        var value = GetProperty(e, name);
        return value is null ? null : value.Value.GetInt32();
    }

    private static double? GetDoubleProperty(LearningEvent e, string name)
    {
        var value = GetProperty(e, name);
        if (value is null)
        {
            return null;
        }

        return value.Value.ValueKind == JsonValueKind.Number ? value.Value.GetDouble() : null;
    }

    private static bool? GetBoolProperty(LearningEvent e, string name)
    {
        var value = GetProperty(e, name);
        return value is null ? null : value.Value.GetBoolean();
    }

    private static JsonElement? GetProperty(LearningEvent e, string name)
    {
        if (string.IsNullOrWhiteSpace(e.PropertiesJson))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(e.PropertiesJson);
        return doc.RootElement.TryGetProperty(name, out var value) ? value.Clone() : null;
    }
}
