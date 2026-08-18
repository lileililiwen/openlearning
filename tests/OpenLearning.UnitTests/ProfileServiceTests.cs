using System;
using Microsoft.AspNetCore.Identity;
using Moq;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using Xunit;

namespace OpenLearning.UnitTests.Auth;

public sealed class ProfileServiceTests
{
    private static (ProfileService Service, Mock<UserManager<ApplicationUser>> Manager) CreateService()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new ProfileService(manager.Object);
        return (service, manager);
    }

    private static ApplicationUser SampleUser()
    {
        return new ApplicationUser
        {
            Id = "u1",
            UserName = "student@openlearning.dev",
            DisplayName = "Old Name",
        };
    }

    [Fact]
    public async Task UpdateProfile_rejects_empty_display_name()
    {
        var (service, _) = CreateService();

        var (ok, error) = await service.UpdateProfileAsync("u1", "   ", "bio", null);

        Assert.False(ok);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateProfile_rejects_bio_over_2000_characters()
    {
        var (service, _) = CreateService();

        var (ok, error) = await service.UpdateProfileAsync("u1", "Name", new string('x', 2001), null);

        Assert.False(ok);
        Assert.Contains("2000", error);
    }

    [Fact]
    public async Task UpdateProfile_rejects_avatar_over_500_characters()
    {
        var (service, _) = CreateService();

        var (ok, error) = await service.UpdateProfileAsync("u1", "Name", null, "https://x/" + new string('y', 500));

        Assert.False(ok);
        Assert.Contains("500", error);
    }

    [Fact]
    public async Task UpdateProfile_returns_error_when_user_not_found()
    {
        var (service, manager) = CreateService();
        manager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var (ok, error) = await service.UpdateProfileAsync("missing", "Name", null, null);

        Assert.False(ok);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateProfile_persists_trimmed_values_on_success()
    {
        var (service, manager) = CreateService();
        var user = SampleUser();
        manager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        manager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var (ok, error) = await service.UpdateProfileAsync("u1", "  New Name  ", "  bio  ", "  https://avatar  ");

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal("bio", user.Bio);
        Assert.Equal("https://avatar", user.AvatarUrl);
    }

    [Fact]
    public async Task UpdateProfile_returns_identity_errors_when_update_fails()
    {
        var (service, manager) = CreateService();
        manager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(SampleUser());
        manager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var (ok, error) = await service.UpdateProfileAsync("u1", "Name", null, null);

        Assert.False(ok);
        Assert.Equal("boom", error);
    }

    [Fact]
    public async Task ChangePassword_returns_error_when_user_not_found()
    {
        var (service, manager) = CreateService();
        manager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var (ok, error) = await service.ChangePasswordAsync("missing", "old", "new");

        Assert.False(ok);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePassword_propagates_identity_result()
    {
        var (service, manager) = CreateService();
        manager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(SampleUser());
        manager.Setup(m => m.ChangePasswordAsync(It.IsAny<ApplicationUser>(), "old", "new"))
            .ReturnsAsync(IdentityResult.Success);

        var (ok, error) = await service.ChangePasswordAsync("u1", "old", "new");

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public async Task ChangePassword_returns_error_descriptions_on_failure()
    {
        var (service, manager) = CreateService();
        manager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(SampleUser());
        manager.Setup(m => m.ChangePasswordAsync(It.IsAny<ApplicationUser>(), "old", "new"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "wrong password" }));

        var (ok, error) = await service.ChangePasswordAsync("u1", "old", "new");

        Assert.False(ok);
        Assert.Equal("wrong password", error);
    }
}
