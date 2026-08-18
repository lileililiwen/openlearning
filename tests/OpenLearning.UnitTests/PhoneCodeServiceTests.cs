using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.Auth;

public sealed class PhoneCodeServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Fact]
    public async Task IssueAsync_creates_six_digit_code()
    {
        var db = CreateDb();
        var service = new PhoneCodeService(db);

        var (ok, error, code) = await service.IssueAsync("+15551234567");

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public async Task VerifyAsync_succeeds_with_correct_code_and_consumes_it()
    {
        var db = CreateDb();
        var service = new PhoneCodeService(db);
        var (_, _, code) = await service.IssueAsync("+15551234567");
        Assert.NotNull(code);
        var issuedCode = code;

        var (ok, error) = await service.VerifyAsync("+15551234567", issuedCode);

        Assert.True(ok);
        Assert.Null(error);
        var record = await db.Set<PhoneCode>().SingleAsync();
        Assert.NotNull(record.UsedAt);

        // Reusing the same code fails.
        var (again, _) = await service.VerifyAsync("+15551234567", issuedCode);
        Assert.False(again);
    }

    [Fact]
    public async Task VerifyAsync_rejects_wrong_code_and_locks_after_five()
    {
        var db = CreateDb();
        var service = new PhoneCodeService(db);
        await service.IssueAsync("+15551234567");

        for (var i = 0; i < 5; i++)
        {
            var (ok, error) = await service.VerifyAsync("+15551234567", "000000");
            Assert.False(ok);
            Assert.NotNull(error);
        }

        var (final, finalError) = await service.VerifyAsync("+15551234567", "000000");
        Assert.False(final);
        Assert.Contains("failed attempts", finalError, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyAsync_rejects_expired_code()
    {
        var db = CreateDb();
        var service = new PhoneCodeService(db);
        var (_, _, code) = await service.IssueAsync("+15551234567");
        Assert.NotNull(code);
        var issuedCode = code;
        var record = await db.Set<PhoneCode>().SingleAsync();
        record.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var (ok, error) = await service.VerifyAsync("+15551234567", issuedCode);

        Assert.False(ok);
        Assert.Contains("expired", error, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IssueAsync_replaces_outstanding_code_for_phone()
    {
        var db = CreateDb();
        var service = new PhoneCodeService(db);
        await service.IssueAsync("+15551234567");
        var (_, _, secondCode) = await service.IssueAsync("+15551234567");

        var records = await db.Set<PhoneCode>().ToListAsync();
        Assert.Single(records);
        Assert.Equal(secondCode, records[0].Code);
    }
}
