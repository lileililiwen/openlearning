using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Logging.Models;
using OpenLearning.Logging.Services;
using Xunit;

namespace OpenLearning.UnitTests.Logging;

public sealed class LogServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Fact]
    public async Task RecordAsync_stores_an_operation_with_actor_and_action()
    {
        var db = CreateDb();
        var service = new LogService(db);

        await service.RecordAsync("a1", "Admin User", "SuspendUser", "User", "u42", null, "127.0.0.1");

        var entry = await db.Set<OperationLog>().SingleAsync();
        Assert.Equal("a1", entry.ActorId);
        Assert.Equal("SuspendUser", entry.Action);
        Assert.Equal("User", entry.TargetType);
        Assert.Equal("u42", entry.TargetId);
    }

    [Fact]
    public async Task LogErrorAsync_stores_the_message_and_path()
    {
        var db = CreateDb();
        var service = new LogService(db);

        await service.LogErrorAsync("boom", "stack", "/Courses/1", "POST", "u1");

        var entry = await db.Set<ErrorLog>().SingleAsync();
        Assert.Equal("boom", entry.Message);
        Assert.Equal("/Courses/1", entry.Path);
        Assert.Equal("POST", entry.RequestMethod);
        Assert.Equal("u1", entry.UserId);
    }

    [Fact]
    public async Task GetOperationsAsync_filters_by_action_and_actor()
    {
        var db = CreateDb();
        var service = new LogService(db);
        await service.RecordAsync(null, "admin@openlearning.dev", "PublishCourse", "Course", "1", null, null);
        await service.RecordAsync(null, "admin@openlearning.dev", "DeleteCourse", "Course", "2", null, null);
        await service.RecordAsync(null, "other@openlearning.dev", "PublishCourse", "Course", "3", null, null);

        var (publish, total) = await service.GetOperationsAsync("PublishCourse", null, null, null, 1, 50);
        var (byActor, _) = await service.GetOperationsAsync(null, "other@openlearning.dev", null, null, 1, 50);

        Assert.Equal(2, total);
        Assert.Equal(2, publish.Count);
        Assert.Single(byActor);
        Assert.Equal("3", byActor[0].TargetId);
    }

    [Fact]
    public async Task PruneAsync_deletes_rows_older_than_the_retention_period()
    {
        var db = CreateDb();
        var service = new LogService(db);
        await service.RecordAsync(null, "admin@openlearning.dev", "PublishCourse", "Course", "1", null, null);
        await db.Set<OperationLog>().AddAsync(new OperationLog
        {
            ActorName = "old",
            Action = "OldAction",
            CreatedAt = DateTime.UtcNow.AddDays(-100),
        });
        await db.SaveChangesAsync();
        await db.Set<ErrorLog>().AddAsync(new ErrorLog
        {
            Message = "old error",
            CreatedAt = DateTime.UtcNow.AddDays(-100),
        });
        await db.SaveChangesAsync();

        var removed = await service.PruneAsync(90);

        Assert.Equal(2, removed);
        Assert.Single(db.Set<OperationLog>());
        Assert.Empty(db.Set<ErrorLog>());
    }
}
