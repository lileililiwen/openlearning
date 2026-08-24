using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Analytics.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Analytics.Services;

/// <summary>Course completion funnel for a selected course and period.</summary>
public sealed record FunnelReport(
    int CourseId,
    string CourseTitle,
    int Eligible,
    int Enrolled,
    int Started,
    int Completed,
    double CompletionRate);

/// <summary>Engagement summary for a selected course and period.</summary>
public sealed record EngagementReport(
    int CourseId,
    string CourseTitle,
    int ActiveLearners,
    long ActiveSeconds);

/// <summary>Cohort retention for a selected cohort.</summary>
public sealed record CohortRetentionReport(
    int CourseId,
    int ClassGroupId,
    string CohortName,
    int Retained,
    bool Suppressed);

/// <summary>Assessment performance for a selected assessment.</summary>
public sealed record AssessmentReport(
    int AssessmentId,
    int CourseId,
    int Attempts,
    int Completions,
    double AverageScore,
    double PassRate);

/// <summary>Instructor teaching workload for an owned course.</summary>
public sealed record WorkloadReport(
    int CourseId,
    string CourseTitle,
    double TeachingHours,
    int GradingWorkload);

/// <summary>Freshness metadata shown on reports.</summary>
public sealed record FreshnessInfo(DateTime? LastSuccessfulRefresh, bool HasData);

/// <summary>
/// Serves authorized analytics reports and exports. Admin reports span all
/// courses; instructor reports are restricted to owned courses. Segments below
/// the configured cohort threshold are suppressed, and exports are audited.
/// </summary>
public class AnalyticsReportService
{
    private readonly DbContext _db;
    private readonly AnalyticsAggregateService _aggregates;

    public AnalyticsReportService(DbContext db, AnalyticsAggregateService aggregates)
    {
        _db = db;
        _aggregates = aggregates;
    }

    public async Task<FreshnessInfo> GetFreshnessAsync()
    {
        var last = await _aggregates.GetLastSuccessfulRefreshAsync();
        var hasData = await _db.Set<CourseFunnelAggregate>().AsNoTracking().AnyAsync();
        return new FreshnessInfo(last, hasData);
    }

    public async Task<FunnelReport?> GetFunnelAsync(int courseId, DateOnly from, DateOnly to)
    {
        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return null;
        }

        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return null;
        }

        var rows = await _db.Set<CourseFunnelAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.CourseId == courseId && a.Date >= from && a.Date <= to)
            .ToListAsync();
        var eligible = rows.Sum(r => r.Eligible);
        var enrolled = rows.Sum(r => r.Enrolled);
        var started = rows.Sum(r => r.Started);
        var completed = rows.Sum(r => r.Completed);
        return new FunnelReport(
            courseId, course.Title, eligible, enrolled, started, completed,
            eligible == 0 ? 0 : (double)completed / eligible);
    }

    public async Task<EngagementReport?> GetEngagementAsync(int courseId, DateOnly from, DateOnly to)
    {
        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return null;
        }

        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return null;
        }

        var rows = await _db.Set<EngagementAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.CourseId == courseId && a.Date >= from && a.Date <= to)
            .ToListAsync();
        return new EngagementReport(
            courseId, course.Title,
            rows.Sum(r => r.ActiveLearners),
            rows.Sum(r => r.ActiveSeconds));
    }

    public async Task<CohortRetentionReport?> GetCohortRetentionAsync(int courseId, int classGroupId, DateOnly from, DateOnly to)
    {
        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return null;
        }

        var cohort = await _db.Set<OpenLearning.Classes.Models.ClassGroup>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classGroupId && c.CourseId == courseId);
        if (cohort is null)
        {
            return null;
        }

        var rows = await _db.Set<CohortRetentionAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.CourseId == courseId && a.ClassGroupId == classGroupId)
            .ToListAsync();
        var retained = rows.Sum(r => r.Retained);
        var threshold = await GetCohortThresholdAsync();
        var suppressed = retained < threshold;
        return new CohortRetentionReport(courseId, classGroupId, cohort.Name, retained, suppressed);
    }

    public async Task<AssessmentReport?> GetAssessmentAsync(int assessmentId, DateOnly from, DateOnly to)
    {
        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return null;
        }

        var rows = await _db.Set<AssessmentAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.AssessmentId == assessmentId && a.Date >= from && a.Date <= to)
            .ToListAsync();
        if (rows.Count == 0)
        {
            return null;
        }

        var attempts = rows.Sum(r => r.Attempts);
        var completions = rows.Sum(r => r.Completions);
        var weightedScore = rows.Sum(r => r.AverageScore * r.Completions);
        var weightedPass = rows.Sum(r => r.PassRate * r.Completions);
        return new AssessmentReport(
            assessmentId,
            rows[0].CourseId,
            attempts,
            completions,
            completions == 0 ? 0 : weightedScore / completions,
            completions == 0 ? 0 : weightedPass / completions);
    }

    /// <summary>
    /// Instructor workload for a course they own. Returns an error if the
    /// instructor does not own the course (denial without leaking metrics).
    /// </summary>
    public async Task<(WorkloadReport? Report, string? Error)> GetInstructorWorkloadAsync(
        string instructorId, int courseId, DateOnly from, DateOnly to)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (null, "Course not found.");
        }

        if (!string.Equals(course.InstructorId, instructorId, StringComparison.Ordinal))
        {
            return (null, "You are not authorized to view this course's analytics.");
        }

        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return (new WorkloadReport(courseId, course.Title, 0, 0), null);
        }

        var rows = await _db.Set<WorkloadAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.CourseId == courseId && a.Date >= from && a.Date <= to)
            .ToListAsync();
        return (new WorkloadReport(
            courseId, course.Title,
            rows.Sum(r => r.TeachingHours),
            rows.Sum(r => r.GradingWorkload)), null);
    }

    /// <summary>
    /// Instructor engagement for an owned course. Denies non-owners.
    /// </summary>
    public async Task<(EngagementReport? Report, string? Error)> GetInstructorEngagementAsync(
        string instructorId, int courseId, DateOnly from, DateOnly to)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (null, "Course not found.");
        }

        if (!string.Equals(course.InstructorId, instructorId, StringComparison.Ordinal))
        {
            return (null, "You are not authorized to view this course's analytics.");
        }

        var report = await GetEngagementAsync(courseId, from, to);
        return (report ?? new EngagementReport(courseId, course.Title, 0, 0), null);
    }

    /// <summary>
    /// Instructor assessment quality for an owned course. Denies non-owners.
    /// </summary>
    public async Task<(List<AssessmentReport> Reports, string? Error)> GetInstructorAssessmentsAsync(
        string instructorId, int courseId, DateOnly from, DateOnly to)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (new List<AssessmentReport>(), "Course not found.");
        }

        if (!string.Equals(course.InstructorId, instructorId, StringComparison.Ordinal))
        {
            return (new List<AssessmentReport>(), "You are not authorized to view this course's analytics.");
        }

        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return (new List<AssessmentReport>(), null);
        }

        var rows = await _db.Set<AssessmentAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.CourseId == courseId && a.Date >= from && a.Date <= to)
            .ToListAsync();
        var reports = rows
            .GroupBy(r => r.AssessmentId)
            .Select(g =>
            {
                var attempts = g.Sum(r => r.Attempts);
                var completions = g.Sum(r => r.Completions);
                var weightedScore = g.Sum(r => r.AverageScore * r.Completions);
                var weightedPass = g.Sum(r => r.PassRate * r.Completions);
                return new AssessmentReport(
                    g.Key, courseId, attempts, completions,
                    completions == 0 ? 0 : weightedScore / completions,
                    completions == 0 ? 0 : weightedPass / completions);
            })
            .ToList();
        return (reports, null);
    }

    /// <summary>Lists courses owned by an instructor for the instructor dashboard.</summary>
    public Task<List<Course>> GetOwnedCoursesAsync(string instructorId)
    {
        return _db.Set<Course>().AsNoTracking()
            .Where(c => c.InstructorId == instructorId)
            .OrderBy(c => c.Title)
            .ToListAsync();
    }

    /// <summary>Lists all courses for the admin dashboard filter.</summary>
    public Task<List<Course>> GetAllCoursesAsync()
    {
        return _db.Set<Course>().AsNoTracking()
            .OrderBy(c => c.Title)
            .ToListAsync();
    }

    /// <summary>Assessment performance for all assessments in a course (admin scope).</summary>
    public async Task<List<AssessmentReport>> GetAdminAssessmentsAsync(int courseId, DateOnly from, DateOnly to)
    {
        var runId = await _aggregates.GetLatestSucceededRunIdAsync();
        if (runId is null)
        {
            return new List<AssessmentReport>();
        }

        var rows = await _db.Set<AssessmentAggregate>().AsNoTracking()
            .Where(a => a.RefreshRunId == runId && a.CourseId == courseId && a.Date >= from && a.Date <= to)
            .ToListAsync();
        return rows
            .GroupBy(r => r.AssessmentId)
            .Select(g =>
            {
                var attempts = g.Sum(r => r.Attempts);
                var completions = g.Sum(r => r.Completions);
                var weightedScore = g.Sum(r => r.AverageScore * r.Completions);
                var weightedPass = g.Sum(r => r.PassRate * r.Completions);
                return new AssessmentReport(
                    g.Key, courseId, attempts, completions,
                    completions == 0 ? 0 : weightedScore / completions,
                    completions == 0 ? 0 : weightedPass / completions);
            })
            .ToList();
    }

    /// <summary>Lists cohorts for a course for the admin dashboard filter.</summary>
    public Task<List<OpenLearning.Classes.Models.ClassGroup>> GetCohortsAsync(int courseId)
    {
        return _db.Set<OpenLearning.Classes.Models.ClassGroup>().AsNoTracking()
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Records an export audit entry. Returns the audit id.
    /// </summary>
    public async Task<long> AuditExportAsync(string requesterId, string scope, object filters)
    {
        var audit = new ExportAudit
        {
            RequesterId = requesterId,
            Scope = scope,
            FiltersJson = JsonSerializer.Serialize(filters),
            ExportedAt = DateTime.UtcNow,
        };
        _db.Set<ExportAudit>().Add(audit);
        await _db.SaveChangesAsync();
        return audit.Id;
    }

    /// <summary>Prunes learning events older than the configured retention.</summary>
    public async Task<int> PruneEventsAsync()
    {
        var retentionDays = await GetRetentionDaysAsync();
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var stale = await _db.Set<LearningEvent>()
            .Where(e => e.ReceivedAt < cutoff)
            .ToListAsync();
        if (stale.Count == 0)
        {
            return 0;
        }

        _db.Set<LearningEvent>().RemoveRange(stale);
        await _db.SaveChangesAsync();
        return stale.Count;
    }

    private async Task<int> GetCohortThresholdAsync()
    {
        var policy = await _db.Set<RetentionPolicy>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Key == "learning-events");
        return policy?.CohortThreshold ?? 5;
    }

    private async Task<int> GetRetentionDaysAsync()
    {
        var policy = await _db.Set<RetentionPolicy>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Key == "learning-events");
        return policy?.RetentionDays ?? 365;
    }
}
