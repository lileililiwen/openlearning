using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using Xunit;

namespace OpenLearning.UnitTests.Notifications;

public sealed class PushSubscriptionServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Fact]
    public async Task SubscribeAsync_stores_and_upserts_same_endpoint()
    {
        var db = CreateDb();
        var service = new PushSubscriptionService(db);

        await service.SubscribeAsync("u1", "https://push.example/1", "p256-1", "auth-1");
        await service.SubscribeAsync("u1", "https://push.example/1", "p256-2", "auth-2");

        var subs = await service.GetForUserAsync("u1");
        Assert.Single(subs);
        Assert.Equal("p256-2", subs[0].P256Dh);
    }

    [Fact]
    public async Task UnsubscribeAsync_removes_only_owned_endpoint()
    {
        var db = CreateDb();
        var service = new PushSubscriptionService(db);
        await service.SubscribeAsync("u1", "https://push.example/1", "a", "b");
        await service.SubscribeAsync("u2", "https://push.example/2", "a", "b");

        var removed = await service.UnsubscribeAsync("u1", "https://push.example/1");

        Assert.True(removed);
        Assert.Empty(await service.GetForUserAsync("u1"));
        Assert.Single(await service.GetForUserAsync("u2"));
    }

    [Fact]
    public async Task UnsubscribeAsync_returns_false_for_unknown_endpoint()
    {
        var db = CreateDb();
        var service = new PushSubscriptionService(db);

        var removed = await service.UnsubscribeAsync("u1", "https://push.example/unknown");

        Assert.False(removed);
    }
}
