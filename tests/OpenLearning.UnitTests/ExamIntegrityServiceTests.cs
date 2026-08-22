using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;
using Xunit;

namespace OpenLearning.UnitTests;

public class ExamIntegrityServiceTests
{
    private static (ApplicationDbContext Db, ExamIntegrityService Service) Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var config = new ConfigurationBuilder().Build();
        var service = new ExamIntegrityService(db, null!, config);
        return (db, service);
    }

    private static async Task<(ApplicationDbContext Db, ExamIntegrityService Service, int AttemptId, string Token, int SessionId)>
        SeedAttemptAsync(string studentId = "student1")
    {
        var (db, service) = Create();
        var course = new Course { Id = 1, InstructorId = "instructor1", Title = "C" };
        var exam = new Exam { Id = 1, CourseId = 1, DurationMinutes = 30, Title = "E" };
        var attempt = new ExamAttempt { Id = 1, ExamId = 1, StudentId = studentId, Status = ExamAttemptStatus.InProgress };
        db.Set<Course>().Add(course);
        db.Set<Exam>().Add(exam);
        db.Set<ExamAttempt>().Add(attempt);
        await db.SaveChangesAsync();

        var (session, token) = await service.BeginSessionAsync(1, studentId);
        return (db, service, 1, token, session.Id);
    }

    [Fact]
    public async Task Replayed_batch_is_not_counted_twice()
    {
        var (db, service, _, token, sessionId) = await SeedAttemptAsync();

        var events = new List<EvidenceInput>
        {
            new(1, IntegrityEventType.VisibilityHidden, DateTime.UtcNow, null),
            new(2, IntegrityEventType.CopyAttempt, DateTime.UtcNow, null),
        };
        var first = await service.IngestAsync(sessionId, token, "batch-1", events);
        Assert.True(first.Accepted);
        Assert.Equal(2, first.AcceptedCount);

        var replay = await service.IngestAsync(sessionId, token, "batch-1", events);
        Assert.True(replay.Replayed);
        Assert.Equal(0, replay.AcceptedCount);
        Assert.Equal(2, await db.IntegrityEvidence.CountAsync(e => e.SessionId == sessionId));
    }

    [Fact]
    public async Task Out_of_order_sequence_is_ignored()
    {
        var (db, service, _, token, sessionId) = await SeedAttemptAsync();

        var first = await service.IngestAsync(sessionId, token, "b1", new[] { new EvidenceInput(2, IntegrityEventType.TabSwitch, DateTime.UtcNow, null) });
        Assert.Equal(1, first.AcceptedCount);
        var second = await service.IngestAsync(sessionId, token, "b2", new[] { new EvidenceInput(1, IntegrityEventType.TabSwitch, DateTime.UtcNow, null) });

        Assert.Equal(0, second.AcceptedCount);
        Assert.Equal(1, await db.IntegrityEvidence.CountAsync(e => e.SessionId == sessionId));
        Assert.Equal(2, (await db.Set<IntegritySession>().FindAsync(sessionId))!.LastSequence);
    }

    [Fact]
    public async Task Server_deadline_is_authoritative_not_client_clock()
    {
        var (_, service, _, _, _) = await SeedAttemptAsync();
        var (session, _) = await service.BeginSessionAsync(1, "student1");

        var expectedMax = DateTime.UtcNow.AddMinutes(30).AddSeconds(5);
        Assert.True(session.ExpiresAt > DateTime.UtcNow);
        Assert.True(session.ExpiresAt <= expectedMax);
    }

    [Fact]
    public async Task Disconnect_then_ingest_is_rejected_but_reconnect_returns_same_session()
    {
        var (db, service, _, token, sessionId) = await SeedAttemptAsync();

        // Reconnect while the session is still active returns the same session.
        var (reconnected, _) = await service.BeginSessionAsync(1, "student1");
        Assert.Equal(sessionId, reconnected.Id);
        Assert.Equal(IntegritySessionStatus.Active, reconnected.Status);

        // After the session is closed, ingestion is rejected.
        var session = await db.Set<IntegritySession>().FindAsync(sessionId);
        session!.Status = IntegritySessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var rejected = await service.IngestAsync(sessionId, token, "b1", new[] { new EvidenceInput(1, IntegrityEventType.Heartbeat, DateTime.UtcNow, null) });
        Assert.False(rejected.Accepted);
    }

    [Fact]
    public async Task Accommodation_extends_session_deadline()
    {
        var (db, service) = Create();
        db.Set<Course>().Add(new Course { Id = 1, InstructorId = "instructor1" });
        db.Set<Exam>().Add(new Exam { Id = 1, CourseId = 1, DurationMinutes = 30 });
        db.Set<ExamAttempt>().Add(new ExamAttempt { Id = 1, ExamId = 1, StudentId = "student1", Status = ExamAttemptStatus.InProgress });
        await db.SaveChangesAsync();

        await service.GrantAccommodationAsync(1, "student1", extraMinutes: 15, allowedBreaks: 2, relaxedVisibilityThreshold: 3, relaxedCopyPasteThreshold: 3, grantedById: "instructor1");

        var (session, _) = await service.BeginSessionAsync(1, "student1");
        var expectedMax = DateTime.UtcNow.AddMinutes(45).AddSeconds(5);
        Assert.True(session.ExpiresAt <= expectedMax);
        Assert.True(session.ExpiresAt > DateTime.UtcNow.AddMinutes(30));
    }

    [Fact]
    public async Task High_risk_queues_incident_without_changing_grade()
    {
        var (db, service, _, token, sessionId) = await SeedAttemptAsync();

        var events = new List<EvidenceInput>();
        for (var i = 1; i <= 10; i++)
        {
            events.Add(new EvidenceInput(i, IntegrityEventType.VisibilityHidden, DateTime.UtcNow, null));
        }
        await service.IngestAsync(sessionId, token, "b1", events);

        var incident = await service.EvaluateAndQueueAsync(1);
        Assert.NotNull(incident);
        Assert.Equal(IntegrityRiskLevel.High, incident.RiskLevel);

        var attempt = await db.Set<ExamAttempt>().FindAsync(1);
        Assert.Equal(ExamAttemptStatus.InProgress, attempt!.Status);
        Assert.Equal(0, attempt.Score);
    }

    [Fact]
    public async Task Reviewer_outside_course_scope_gets_no_incident_details()
    {
        var (_, service, _, token, sessionId) = await SeedAttemptAsync();

        var many = new List<EvidenceInput>();
        for (var i = 1; i <= 10; i++)
        {
            many.Add(new EvidenceInput(i, IntegrityEventType.VisibilityHidden, DateTime.UtcNow, null));
        }
        await service.IngestAsync(sessionId, token, "b1", many);
        var incident = await service.EvaluateAndQueueAsync(1);
        Assert.NotNull(incident);

        var other = await service.GetIncidentForReviewAsync(incident.Id, "other-instructor");
        Assert.Null(other);
        var evidence = await service.GetEvidenceForReviewAsync(incident.Id, "other-instructor");
        Assert.Empty(evidence);
    }

    [Fact]
    public async Task Retention_purges_expired_evidence()
    {
        var (db, service, _, token, sessionId) = await SeedAttemptAsync();

        await service.IngestAsync(sessionId, token, "b1", new[] { new EvidenceInput(1, IntegrityEventType.Heartbeat, DateTime.UtcNow, null) });
        var old = await db.Set<IntegrityEvidence>().FirstAsync();
        old.ReceivedAt = DateTime.UtcNow.AddDays(-100);
        await db.SaveChangesAsync();

        var removed = await service.PurgeExpiredEvidenceAsync();
        Assert.Equal(1, removed);
        Assert.Equal(0, await db.IntegrityEvidence.CountAsync());
    }

    [Fact]
    public async Task Disposition_requires_authorized_reviewer()
    {
        var (_, service, _, token, sessionId) = await SeedAttemptAsync();

        var many = new List<EvidenceInput>();
        for (var i = 1; i <= 10; i++)
        {
            many.Add(new EvidenceInput(i, IntegrityEventType.VisibilityHidden, DateTime.UtcNow, null));
        }
        await service.IngestAsync(sessionId, token, "b1", many);
        var incident = await service.EvaluateAndQueueAsync(1);
        Assert.NotNull(incident);

        var denied = await service.RecordDispositionAsync(incident.Id, "other-instructor", IntegrityDispositionOutcome.NoAction, null);
        Assert.NotNull(denied.Error);

        var ok = await service.RecordDispositionAsync(incident.Id, "instructor1", IntegrityDispositionOutcome.NoAction, "reviewed");
        Assert.Null(ok.Error);
        Assert.NotNull(ok.Disposition);
    }
}
