using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Operations.Models;
using OpenLearning.Operations.Services;
using Xunit;

namespace OpenLearning.UnitTests.Operations;

public sealed class OperationsServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<int> CreateCampaignAsync(
        ApplicationDbContext db, string name, DateTime startsAt, DateTime endsAt, bool isActive = true)
    {
        var service = new OperationsService(db);
        var (ok, error) = await service.CreateCampaignAsync(name, startsAt, endsAt);
        Assert.True(ok);
        Assert.Null(error);
        var campaign = (await service.GetAllCampaignsAsync()).Single(c => c.Name == name);
        if (!isActive)
        {
            await service.ToggleCampaignAsync(campaign.Id);
        }

        return campaign.Id;
    }

    private static readonly string[] _activeBannerTitles = { "Always", "In Window" };

    [Fact]
    public async Task CreateBannerAsync_adds_and_orders()
    {
        var db = CreateDb();
        var service = new OperationsService(db);

        await service.CreateBannerAsync("Spring", "/img/1.png", "/", null);
        await service.CreateBannerAsync("Summer", "/img/2.png", "/", null);

        var banners = await service.GetActiveBannersAsync();
        Assert.Equal(2, banners.Count);
        Assert.Equal("Spring", banners[0].Title);
        Assert.Equal("Summer", banners[1].Title);
    }

    [Fact]
    public async Task GetActiveBannersAsync_filters_inactive_and_campaign_windows()
    {
        var db = CreateDb();
        var service = new OperationsService(db);
        var activeCampaign = await CreateCampaignAsync(
            db, "Live Campaign", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        var expiredCampaign = await CreateCampaignAsync(
            db, "Expired Campaign", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1));

        await service.CreateBannerAsync("Always", "/img/a.png", "/", null);
        await service.CreateBannerAsync("In Window", "/img/b.png", "/", activeCampaign);
        await service.CreateBannerAsync("Expired", "/img/c.png", "/", expiredCampaign);
        await service.CreateBannerAsync("Off", "/img/d.png", "/", null);
        var off = (await service.GetAllBannersAsync()).Single(b => b.Title == "Off");
        await service.UpdateBannerAsync(off.Id, "Off", "/img/d.png", "/", null, isActive: false);

        var active = await service.GetActiveBannersAsync();
        Assert.Equal(_activeBannerTitles, active.Select(b => b.Title).ToArray());
    }

    [Fact]
    public async Task GetActivePopupAsync_returns_only_windowed_popup()
    {
        var db = CreateDb();
        var service = new OperationsService(db);

        await service.CreatePopupAsync(
            "Live", "body", "/", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));
        await service.CreatePopupAsync(
            "Future", "body", "/", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

        var popup = await service.GetActivePopupAsync();
        Assert.NotNull(popup);
        Assert.Equal("Live", popup.Title);
    }

    [Fact]
    public async Task CreatePopupAsync_rejects_backwards_window()
    {
        var db = CreateDb();
        var service = new OperationsService(db);

        var (ok, error) = await service.CreatePopupAsync(
            "Bad", "body", "/", DateTime.UtcNow.AddDays(1), DateTime.UtcNow);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SetHomepageFeaturesAsync_replaces_features()
    {
        var db = CreateDb();
        var service = new OperationsService(db);

        await service.SetHomepageFeaturesAsync(new[]
        {
            (Category: (string?)null, CourseId: (int?)1),
            (Category: (string?)"Programming", CourseId: (int?)null),
        });

        var features = await service.GetHomepageFeaturesAsync();
        Assert.Equal(2, features.Count);
        Assert.Equal(1, features[0].CourseId);
        Assert.Equal("Programming", features[1].Category);
    }
}
