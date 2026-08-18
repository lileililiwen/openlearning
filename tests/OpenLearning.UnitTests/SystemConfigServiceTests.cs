using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Notifications.Models;
using OpenLearning.SystemConfig.Models;
using OpenLearning.SystemConfig.Services;
using Xunit;

namespace OpenLearning.UnitTests.SystemConfig;

public sealed class SystemConfigServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Fact]
    public async Task SetAsync_then_GetStringAsync_roundtrips()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        await service.SetAsync("Site.Name", "My Platform");
        var value = await service.GetStringAsync("Site.Name", "fallback");

        Assert.Equal("My Platform", value);
    }

    [Fact]
    public async Task SetAsync_upserts_the_same_key()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        await service.SetAsync("Site.Name", "First");
        await service.SetAsync("Site.Name", "Second");

        Assert.Equal(1, await db.Set<Setting>().CountAsync());
        Assert.Equal("Second", await service.GetStringAsync("Site.Name", "fallback"));
    }

    [Fact]
    public async Task GetStringAsync_returns_fallback_when_unset_or_empty()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        Assert.Equal("fallback", await service.GetStringAsync("Missing.Key", "fallback"));

        await service.SetAsync("Empty.Key", "  ");
        Assert.Equal("fallback", await service.GetStringAsync("Empty.Key", "fallback"));
    }

    [Fact]
    public async Task GetIntAsync_parses_valid_values_and_falls_back_on_bad_ones()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        await service.SetAsync("Catalog.PageSize", "7");
        Assert.Equal(7, await service.GetIntAsync("Catalog.PageSize", 9));

        await service.SetAsync("Catalog.PageSize", "not-a-number");
        Assert.Equal(9, await service.GetIntAsync("Catalog.PageSize", 9));

        Assert.Equal(9, await service.GetIntAsync("Missing.Key", 9));
    }

    [Fact]
    public async Task GetBoolAsync_parses_true_and_falls_back()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        await service.SetAsync("Flag", "true");
        Assert.True(await service.GetBoolAsync("Flag", false));

        await service.SetAsync("Flag", "maybe");
        Assert.False(await service.GetBoolAsync("Flag", false));
    }

    [Fact]
    public async Task SetManyAsync_writes_all_values()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        await service.SetManyAsync(new Dictionary<string, string>
        {
            ["Site.Name"] = "A",
            ["Catalog.PageSize"] = "12",
        });

        Assert.Equal("A", await service.GetStringAsync("Site.Name", "x"));
        Assert.Equal(12, await service.GetIntAsync("Catalog.PageSize", 9));
    }

    [Fact]
    public async Task RenderAsync_returns_null_when_no_template_exists()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);

        var rendered = await service.RenderAsync(NotificationType.Course, "t", "b", null);

        Assert.Null(rendered);
    }

    [Fact]
    public async Task RenderAsync_substitutes_known_tokens()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);
        db.Set<NotificationTemplate>().Add(new NotificationTemplate
        {
            Type = NotificationType.Announcement,
            Title = "New announcement in {CourseTitle}",
            Body = "{Message}",
        });
        await db.SaveChangesAsync();

        var rendered = await service.RenderAsync(
            NotificationType.Announcement, "t", "b",
            new Dictionary<string, string> { ["CourseTitle"] = "C# 101", ["Message"] = "Welcome!" });

        Assert.NotNull(rendered);
        Assert.Equal("New announcement in C# 101", rendered.Value.Title);
        Assert.Equal("Welcome!", rendered.Value.Body);
    }

    [Fact]
    public async Task RenderAsync_ignores_inactive_templates_and_keeps_unknown_tokens()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);
        db.Set<NotificationTemplate>().Add(new NotificationTemplate
        {
            Type = NotificationType.Quiz,
            Title = "Quiz {QuizTitle} {Unknown}",
            Body = "Score {Score}",
            IsActive = false,
        });
        await db.SaveChangesAsync();

        Assert.Null(await service.RenderAsync(NotificationType.Quiz, "t", "b", null));

        var quizTemplate = await db.Set<NotificationTemplate>().SingleAsync();
        quizTemplate.IsActive = true;
        await db.SaveChangesAsync();

        var rendered = await service.RenderAsync(
            NotificationType.Quiz, "t", "b",
            new Dictionary<string, string> { ["QuizTitle"] = "Midterm", ["Score"] = "80" });

        Assert.NotNull(rendered);
        Assert.Equal("Quiz Midterm {Unknown}", rendered.Value.Title);
        Assert.Equal("Score 80", rendered.Value.Body);
    }

    [Fact]
    public async Task GetTemplatesAsync_returns_all_ordered_by_type()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);
        db.Set<NotificationTemplate>().AddRange(
            new NotificationTemplate { Type = NotificationType.Quiz, Title = "q", Body = "b" },
            new NotificationTemplate { Type = NotificationType.Course, Title = "c", Body = "b" });
        await db.SaveChangesAsync();

        var templates = await service.GetTemplatesAsync();

        Assert.Equal(new[] { NotificationType.Course, NotificationType.Quiz }, templates.Select(t => t.Type));
    }

    [Fact]
    public async Task UpdateTemplateAsync_changes_title_body_and_activation()
    {
        var db = CreateDb();
        var service = new SystemConfigService(db);
        var template = new NotificationTemplate { Type = NotificationType.Lesson, Title = "old", Body = "old body" };
        db.Set<NotificationTemplate>().Add(template);
        await db.SaveChangesAsync();

        await service.UpdateTemplateAsync(template.Id, "new title", "new body", false);

        var updated = await db.Set<NotificationTemplate>().SingleAsync();
        Assert.Equal("new title", updated.Title);
        Assert.Equal("new body", updated.Body);
        Assert.False(updated.IsActive);
    }
}
