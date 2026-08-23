using Microsoft.EntityFrameworkCore;
using OpenLearning.Credits.Models;
using OpenLearning.Credits.Services;
using Xunit;

namespace OpenLearning.UnitTests;

public sealed class CreditServiceTests
{
    [Fact]
    public async Task Duplicate_completion_is_awarded_once_and_revocation_is_compensating()
    {
        await using var db = CreateDb();
        var service = new CreditService(db);
        await service.PublishCourseRuleAsync(7, 3m, CreditCategory.Major);

        Assert.NotNull(await service.ProcessCourseCompletionAsync("student", 7));
        Assert.Null(await service.ProcessCourseCompletionAsync("student", 7));
        var award = Assert.Single(await service.GetLedgerAsync("student"));
        await service.RevokeAsync(award.Id, "Incorrect completion", "admin");

        var ledger = await service.GetLedgerAsync("student");
        Assert.Equal(2, ledger.Count);
        Assert.Equal(0m, ledger.Sum(x => x.Amount));
        Assert.Equal("Incorrect completion", ledger.Single(x => x.Amount < 0).Reason);
    }

    [Fact]
    public async Task New_program_version_does_not_move_existing_learner()
    {
        await using var db = CreateDb();
        var service = new CreditService(db);
        var first = await service.CreateProgramAsync("Degree", 10m, [], []);
        await service.AssignProgramAsync("student", first.Id);
        var second = await service.CreateProgramAsync("Degree", 12m, [], []);

        var assigned = await service.GetLearnerProgramAsync("student");
        Assert.Equal(first.Id, assigned!.ProgramId);
        Assert.Equal(1, assigned.Program!.Version);
        Assert.Equal(2, second.Version);
    }

    [Fact]
    public async Task Audit_explains_category_shortfall_and_graduation_rechecks_revocation()
    {
        await using var db = CreateDb();
        var service = new CreditService(db);
        var program = await service.CreateProgramAsync("Degree", 3m,
            new() { [CreditCategory.Major] = 3m }, ["7"]);
        await service.AssignProgramAsync("student", program.Id);
        await service.PublishCourseRuleAsync(7, 3m, CreditCategory.Major);

        var initial = await service.EvaluateAsync("student");
        Assert.False(initial.Eligible);
        Assert.Contains(initial.UnmetRequirements, x => x.Contains("Major", StringComparison.Ordinal));
        var award = await service.ProcessCourseCompletionAsync("student", 7);
        Assert.True((await service.EvaluateAsync("student")).Eligible);
        await service.RevokeAsync(award!.Id, "Correction", "admin");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GraduateAsync("student", program.Id, "admin", null));
        Assert.Contains("not eligible", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.Set<GraduationDecision>().ToListAsync());
    }

    private static TestDb CreateDb()
    {
        return new(new DbContextOptionsBuilder<TestDb>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CreditAward>();
            modelBuilder.Entity<CourseCreditRule>();
            modelBuilder.Entity<GraduationProgram>();
            modelBuilder.Entity<LearnerProgram>().Ignore(x => x.Student).HasOne(x => x.Program).WithMany();
            modelBuilder.Entity<GraduationDecision>().Ignore(x => x.Student).HasOne(x => x.Program).WithMany();
        }
    }
}
