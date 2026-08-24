using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Analytics.Models;
using OpenLearning.Analytics.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.Analytics;

public sealed class LearningEventServiceTests
{
    private static (ApplicationDbContext Db, LearningEventService Service) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new LearningEventService(db));
    }

    private static LearningEventInput MakeInput(
        string eventType = LearningEventSchema.CourseStarted,
        string eventId = "evt-1",
        string actorKey = "actor-1",
        int? courseId = 1,
        IReadOnlyDictionary<string, JsonElement>? properties = null)
    {
        return new LearningEventInput(
            eventType, actorKey, eventId, courseId, null, null, null,
            DateTime.UtcNow, properties);
    }

    [Fact]
    public async Task Duplicate_event_is_counted_once()
    {
        var (db, service) = Create();
        var first = await service.IngestAsync(MakeInput(eventId: "dup-1"));
        var second = await service.IngestAsync(MakeInput(eventId: "dup-1"));

        Assert.True(first.Accepted);
        Assert.False(first.Duplicate);
        Assert.True(second.Duplicate);
        Assert.Equal(1, await db.Set<LearningEvent>().CountAsync());
    }

    [Fact]
    public async Task Unknown_event_type_is_rejected()
    {
        var (db, service) = Create();
        var result = await service.IngestAsync(MakeInput(eventType: "unknown.type"));

        Assert.False(result.Accepted);
        Assert.Equal(EventValidationOutcome.RejectedUnknownType, result.Outcome);
        Assert.Equal(0, await db.Set<LearningEvent>().CountAsync());
    }

    [Fact]
    public async Task Unknown_property_is_discarded_and_outcome_observable()
    {
        var (db, service) = Create();
        var properties = new Dictionary<string, JsonElement>
        {
            ["seconds"] = JsonDocument.Parse("120").RootElement.Clone(),
            ["secretField"] = JsonDocument.Parse("\"leak\"").RootElement.Clone(),
        };
        var result = await service.IngestAsync(MakeInput(
            eventType: LearningEventSchema.SessionActive,
            properties: properties));

        Assert.True(result.Accepted);
        Assert.Equal(EventValidationOutcome.DiscardedUnknownProperty, result.Outcome);
        var stored = await db.Set<LearningEvent>().SingleAsync();
        Assert.Equal(EventValidationOutcome.DiscardedUnknownProperty, stored.ValidationOutcome);
        Assert.DoesNotContain("secretField", stored.PropertiesJson);
        Assert.Contains("seconds", stored.PropertiesJson);
    }

    [Fact]
    public async Task Pseudonymous_actor_key_is_stored()
    {
        var (db, service) = Create();
        await service.IngestAsync(MakeInput(actorKey: "pseudo-abc123"));

        var stored = await db.Set<LearningEvent>().SingleAsync();
        Assert.Equal("pseudo-abc123", stored.ActorKey);
    }
}

public sealed class AnalyticsAggregateServiceTests
{
    private static (ApplicationDbContext Db, AnalyticsAggregateService Service) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new AnalyticsAggregateService(db));
    }

    private static async Task SeedEventAsync(
        ApplicationDbContext db, string eventType, string actorKey, string eventId,
        int courseId, int? assessmentId = null, int? classGroupId = null,
        IReadOnlyDictionary<string, JsonElement>? properties = null)
    {
        db.Set<LearningEvent>().Add(new LearningEvent
        {
            EventType = eventType,
            ActorKey = actorKey,
            EventId = eventId,
            CourseId = courseId,
            AssessmentId = assessmentId,
            ClassGroupId = classGroupId,
            OccurredAt = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            PropertiesJson = properties is null ? null : JsonSerializer.Serialize(properties),
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Atomic_refresh_serves_only_succeeded_run()
    {
        var (db, service) = Create();
        var day = DateOnly.FromDateTime(DateTime.UtcNow);

        await SeedEventAsync(db, LearningEventSchema.CourseEnrolled, "a1", "e1", 1);
        await SeedEventAsync(db, LearningEventSchema.CourseStarted, "a1", "e2", 1);
        await SeedEventAsync(db, LearningEventSchema.CourseCompleted, "a1", "e3", 1);

        var (runId, error) = await service.RefreshDailyAsync(day);
        Assert.NotNull(runId);
        Assert.Null(error);

        var latest = await service.GetLatestSucceededRunIdAsync();
        Assert.Equal(runId, latest);

        var funnel = await db.Set<CourseFunnelAggregate>().SingleAsync();
        Assert.Equal(runId, funnel.RefreshRunId);
        Assert.Equal(1, funnel.Eligible);
        Assert.Equal(1, funnel.Started);
        Assert.Equal(1, funnel.Completed);
    }

    [Fact]
    public async Task Partial_run_is_never_served()
    {
        var (db, service) = Create();
        var day = DateOnly.FromDateTime(DateTime.UtcNow);

        // A failed run leaves no served facts.
        db.Set<RefreshRun>().Add(new RefreshRun
        {
            Scope = "daily",
            AggregateDate = day,
            Status = RefreshRunStatus.Failed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Error = "boom",
        });
        await db.SaveChangesAsync();

        var latest = await service.GetLatestSucceededRunIdAsync();
        Assert.Null(latest);
        Assert.Equal(0, await db.Set<CourseFunnelAggregate>().CountAsync());
    }

    [Fact]
    public async Task Engagement_and_workload_are_aggregated()
    {
        var (db, service) = Create();
        var day = DateOnly.FromDateTime(DateTime.UtcNow);

        var seconds = new Dictionary<string, JsonElement> { ["seconds"] = JsonDocument.Parse("300").RootElement.Clone() };
        var hours = new Dictionary<string, JsonElement> { ["hours"] = JsonDocument.Parse("1.5").RootElement.Clone() };
        await SeedEventAsync(db, LearningEventSchema.SessionActive, "a1", "e1", 1, properties: seconds);
        await SeedEventAsync(db, LearningEventSchema.SessionActive, "a2", "e2", 1, properties: seconds);
        await SeedEventAsync(db, LearningEventSchema.LiveAttended, "a1", "e3", 1, properties: hours);
        await SeedEventAsync(db, LearningEventSchema.AssessmentCompleted, "a1", "e4", 1, assessmentId: 7);

        var (runId, _) = await service.RefreshDailyAsync(day);
        Assert.NotNull(runId);

        var engagement = await db.Set<EngagementAggregate>().SingleAsync();
        Assert.Equal(2, engagement.ActiveLearners);
        Assert.Equal(600, engagement.ActiveSeconds);

        var workload = await db.Set<WorkloadAggregate>().SingleAsync();
        Assert.Equal(1.5, workload.TeachingHours);
        Assert.Equal(1, workload.GradingWorkload);
    }
}

public sealed class AnalyticsReportServiceTests
{
    private static (ApplicationDbContext Db, AnalyticsReportService Service) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var aggregates = new AnalyticsAggregateService(db);
        return (db, new AnalyticsReportService(db, aggregates));
    }

    private static async Task<int> SeedCourseAsync(ApplicationDbContext db, string instructorId)
    {
        var course = new Course { Title = "Course", InstructorId = instructorId };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        return course.Id;
    }

    private static async Task<long> SeedSucceededRunAsync(ApplicationDbContext db)
    {
        var run = new RefreshRun
        {
            Scope = "daily",
            Status = RefreshRunStatus.Succeeded,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        };
        db.Set<RefreshRun>().Add(run);
        await db.SaveChangesAsync();
        return run.Id;
    }

    [Fact]
    public async Task Instructor_non_owner_course_is_denied()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db, "owner-1");

        var (report, error) = await service.GetInstructorWorkloadAsync("intruder-2", courseId, DateOnly.MinValue, DateOnly.MaxValue);
        Assert.Null(report);
        Assert.NotNull(error);
        Assert.Contains("not authorized", error);
    }

    [Fact]
    public async Task Instructor_owner_course_is_allowed()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db, "owner-1");
        var runId = await SeedSucceededRunAsync(db);
        db.Set<WorkloadAggregate>().Add(new WorkloadAggregate
        {
            RefreshRunId = runId,
            CourseId = courseId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            TeachingHours = 2,
            GradingWorkload = 3,
        });
        await db.SaveChangesAsync();

        var (report, error) = await service.GetInstructorWorkloadAsync("owner-1", courseId, DateOnly.MinValue, DateOnly.MaxValue);
        Assert.Null(error);
        Assert.NotNull(report);
        Assert.Equal(2, report.TeachingHours);
        Assert.Equal(3, report.GradingWorkload);
    }

    [Fact]
    public async Task Small_cohort_is_suppressed()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db, "owner-1");
        var runId = await SeedSucceededRunAsync(db);
        db.Set<RetentionPolicy>().Add(new RetentionPolicy { Key = "learning-events", RetentionDays = 365, CohortThreshold = 5 });
        db.Set<CohortRetentionAggregate>().Add(new CohortRetentionAggregate
        {
            RefreshRunId = runId,
            CourseId = courseId,
            ClassGroupId = 9,
            PeriodIndex = 0,
            Retained = 2,
        });
        db.Set<OpenLearning.Classes.Models.ClassGroup>().Add(new OpenLearning.Classes.Models.ClassGroup
        {
            Id = 9,
            CourseId = courseId,
            Name = "Cohort A",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();

        var report = await service.GetCohortRetentionAsync(courseId, 9, DateOnly.MinValue, DateOnly.MaxValue);
        Assert.NotNull(report);
        Assert.True(report.Suppressed);
    }

    [Fact]
    public async Task Export_is_audited()
    {
        var (db, service) = Create();
        var auditId = await service.AuditExportAsync("user-1", "admin", new { courseId = 1, from = "2026-01-01" });

        var audit = await db.Set<ExportAudit>().SingleAsync();
        Assert.Equal(auditId, audit.Id);
        Assert.Equal("user-1", audit.RequesterId);
        Assert.Equal("admin", audit.Scope);
        Assert.Contains("courseId", audit.FiltersJson);
    }

    [Fact]
    public async Task Funnel_report_returns_defined_denominators()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db, "owner-1");
        var runId = await SeedSucceededRunAsync(db);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Set<CourseFunnelAggregate>().Add(new CourseFunnelAggregate
        {
            RefreshRunId = runId,
            CourseId = courseId,
            Date = day,
            Eligible = 10,
            Enrolled = 10,
            Started = 6,
            Completed = 4,
        });
        await db.SaveChangesAsync();

        var report = await service.GetFunnelAsync(courseId, day, day);
        Assert.NotNull(report);
        Assert.Equal(10, report.Eligible);
        Assert.Equal(10, report.Enrolled);
        Assert.Equal(6, report.Started);
        Assert.Equal(4, report.Completed);
        Assert.Equal(0.4, report.CompletionRate);
    }
}
