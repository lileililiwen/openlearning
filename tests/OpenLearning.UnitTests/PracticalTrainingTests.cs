using Microsoft.EntityFrameworkCore;
using OpenLearning.PracticalTraining.Models;
using OpenLearning.PracticalTraining.Services;
using OpenLearning.Storage.Models;
using Xunit;

namespace OpenLearning.UnitTests;

public sealed class PracticalTrainingTests
{
    [Fact]
    public async Task Expired_invitation_and_cross_placement_identifier_are_denied()
    {
        await using var db = CreateDb();
        var service = new PracticalTrainingService(db);
        var first = await SeedPlacement(service, "learner-1");
        var second = await SeedPlacement(service, "learner-2");
        var expired = await service.InviteSupervisorAsync(first.Id, "coordinator", false, TimeSpan.FromMinutes(-1));
        Assert.Null(await service.ResolveSupervisorAsync(expired.Token!));
        var active = await service.InviteSupervisorAsync(first.Id, "coordinator", false, TimeSpan.FromHours(1));
        var log = (await service.SubmitHoursAsync(second.Id, "learner-2", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1), "work")).Log!;
        var result = await service.ReviewHoursAsync(active.Token!, second.Id, log.Id, log.ConcurrencyStamp, true, null);
        Assert.False(result.Ok);
        Assert.Equal(PracticalLogStatus.Submitted, log.Status);
    }

    [Fact]
    public async Task Approved_log_is_preserved_when_amendment_is_submitted_and_approved()
    {
        await using var db = CreateDb();
        var service = new PracticalTrainingService(db);
        var placement = await SeedPlacement(service, "learner");
        var invite = await service.InviteSupervisorAsync(placement.Id, "coordinator", false, TimeSpan.FromHours(1));
        var start = DateTime.UtcNow.AddDays(-1);
        var original = (await service.SubmitHoursAsync(placement.Id, "learner", start, start.AddHours(4), "original")).Log!;
        Assert.True((await service.ReviewHoursAsync(invite.Token!, placement.Id, original.Id, original.ConcurrencyStamp, true, null)).Ok);
        var amendment = (await service.SubmitHoursAsync(placement.Id, "learner", start, start.AddHours(5), "corrected", original.Id)).Log!;
        Assert.Equal(PracticalLogStatus.Approved, original.Status);
        Assert.Equal(original.Id, amendment.AmendsLogId);
        Assert.True((await service.ReviewHoursAsync(invite.Token!, placement.Id, amendment.Id, amendment.ConcurrencyStamp, true, null)).Ok);
        Assert.Equal(PracticalLogStatus.Superseded, original.Status);
        Assert.Equal(PracticalLogStatus.Approved, amendment.Status);
        Assert.False((await service.ReviewHoursAsync(invite.Token!, placement.Id, amendment.Id, amendment.ConcurrencyStamp, true, null)).Ok);
    }

    [Fact]
    public async Task Completion_rejects_unresolved_blocking_incident_and_incomplete_requirements()
    {
        await using var db = CreateDb();
        var service = new PracticalTrainingService(db);
        var placement = await SeedPlacement(service, "learner", minimumHours: 2);
        var incomplete = await service.ConfirmCompletionAsync(placement.Id, "coordinator", false);
        Assert.False(incomplete.Ok);
        Assert.Contains("hours", incomplete.Error!, StringComparison.OrdinalIgnoreCase);
        db.Add(new PracticalHourLog { PlacementId = placement.Id, StartedAt = DateTime.UtcNow.AddHours(-3), EndedAt = DateTime.UtcNow, Status = PracticalLogStatus.Approved });
        foreach (var competency in await db.Set<PlacementCompetency>().ToListAsync())
            competency.IsAchieved = true;
        db.Add(new PracticalEvaluation { PlacementId = placement.Id, EvaluatorKind = "Supervisor", Summary = "ready" });
        await db.SaveChangesAsync();
        await service.ReportIncidentAsync(placement.Id, "coordinator", false, IncidentSeverity.Blocking, "safety");
        var blocked = await service.ConfirmCompletionAsync(placement.Id, "coordinator", false);
        Assert.False(blocked.Ok);
        Assert.Contains("blocking", blocked.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Evidence_rejects_file_not_owned_by_learner()
    {
        await using var db = CreateDb();
        var service = new PracticalTrainingService(db);
        var placement = await SeedPlacement(service, "learner");
        var file = new StoredFile { Key = "answer/test.pdf", OriginalName = "test.pdf", ContentType = "application/pdf", OwnerId = "other", Purpose = FilePurpose.Answer, SizeBytes = 10, IsPrivate = true };
        db.Add(file);
        await db.SaveChangesAsync();
        var result = await service.AddEvidenceAsync(placement.Id, "learner", file.Id, "evidence");
        Assert.False(result.Ok);
    }

    private static async Task<Placement> SeedPlacement(PracticalTrainingService service, string learner, decimal minimumHours = 10)
    {
        var program = await service.CreateProgramAsync("Program " + Guid.NewGuid(), minimumHours, ["Safety"]);
        var host = await service.CreateHostAsync("Host " + Guid.NewGuid(), "host@example.com");
        var placement = await service.CreatePlacementAsync(program.Id, host.Id, learner, "coordinator", "Supervisor", "supervisor@example.com", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)));
        Assert.True((await service.ActivateAsync(placement.Id, "coordinator", false)).Ok);
        return placement;
    }

    private static TestDb CreateDb()
    {
        return new(new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PracticalProgram>();
            modelBuilder.Entity<ProgramCompetency>();
            modelBuilder.Entity<HostOrganization>();
            modelBuilder.Entity<Placement>();
            modelBuilder.Entity<PlacementCompetency>();
            modelBuilder.Entity<SupervisorInvitation>();
            modelBuilder.Entity<PracticalHourLog>();
            modelBuilder.Entity<PracticalEvidence>();
            modelBuilder.Entity<PracticalEvaluation>();
            modelBuilder.Entity<PlacementIncident>();
            modelBuilder.Entity<PracticalCompletion>();
            modelBuilder.Entity<StoredFile>();
        }
    }
}
