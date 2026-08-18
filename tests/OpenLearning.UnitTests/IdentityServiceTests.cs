using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.Auth;

public sealed class IdentityServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static void AddUser(ApplicationDbContext db, string id, IdentityStatus status)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@example.com",
            Email = $"{id}@example.com",
            DisplayName = id,
            IdentityStatus = status,
            RealName = "Real " + id,
        };
        db.Set<ApplicationUser>().Add(user);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetPendingAsync_returns_only_pending_users()
    {
        var db = CreateDb();
        AddUser(db, "u1", IdentityStatus.Pending);
        AddUser(db, "u2", IdentityStatus.Verified);
        AddUser(db, "u3", IdentityStatus.Unverified);

        var service = new IdentityService(db);
        var pending = await service.GetPendingAsync();

        Assert.Single(pending);
        Assert.Equal("u1", pending[0].Id);
    }

    [Fact]
    public async Task ApproveAsync_sets_verified_with_note()
    {
        var db = CreateDb();
        AddUser(db, "u1", IdentityStatus.Pending);

        var service = new IdentityService(db);
        var (ok, error) = await service.ApproveAsync("u1", "Looks good");

        Assert.True(ok);
        Assert.Null(error);
        var user = await db.Set<ApplicationUser>().SingleAsync();
        Assert.Equal(IdentityStatus.Verified, user.IdentityStatus);
        Assert.NotNull(user.VerifiedAt);
        Assert.Equal("Looks good", user.VerificationNote);
    }

    [Fact]
    public async Task RejectAsync_sets_rejected_and_clears_verified_at()
    {
        var db = CreateDb();
        AddUser(db, "u1", IdentityStatus.Pending);

        var service = new IdentityService(db);
        var (ok, error) = await service.RejectAsync("u1", "Document unclear");

        Assert.True(ok);
        Assert.Null(error);
        var user = await db.Set<ApplicationUser>().SingleAsync();
        Assert.Equal(IdentityStatus.Rejected, user.IdentityStatus);
        Assert.Null(user.VerifiedAt);
        Assert.Equal("Document unclear", user.VerificationNote);
    }

    [Fact]
    public async Task ApproveAsync_unknown_user_returns_error()
    {
        var db = CreateDb();
        var service = new IdentityService(db);

        var (ok, error) = await service.ApproveAsync("missing", null);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
