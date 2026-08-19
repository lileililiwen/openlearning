using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Live;

public sealed class LiveServiceTests
{
    private static (ApplicationDbContext Db, LiveService Service, Course Course) SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "owner-1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        db.Set<ApplicationUser>().AddRange(
            new ApplicationUser { Id = "owner-1", UserName = "owner-1" },
            new ApplicationUser { Id = "student-1", UserName = "student-1" },
            new ApplicationUser { Id = "cohost-1", UserName = "cohost-1", Email = "cohost@example.com" });
        db.SaveChanges();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        return (db, new LiveService(db, config), course);
    }

    private static LiveInput Input()
    {
        var start = DateTime.UtcNow.AddHours(1);
        return new LiveInput("Live Q&A", "Join us", start, start.AddHours(2));
    }

    [Fact]
    public async Task Create_requires_owner_and_valid_window()
    {
        var (db, service, course) = SeedAsync();

        var (nonOwnerOk, nonOwnerError) = await service.CreateAsync(course.Id, "student-1", Input());
        var (badWindowOk, badWindowError) = await service.CreateAsync(course.Id, "owner-1",
            new LiveInput("X", null, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(1)));
        var (ok, error) = await service.CreateAsync(course.Id, "owner-1", Input());

        Assert.False(nonOwnerOk);
        Assert.NotNull(nonOwnerError);
        Assert.False(badWindowOk);
        Assert.NotNull(badWindowError);
        Assert.True(ok);
        Assert.Null(error);
        var session = Assert.Single(db.Set<LiveSession>());
        Assert.Equal("Live Q&A", session.Title);
        Assert.False(string.IsNullOrWhiteSpace(session.StreamKey));
        Assert.Contains(session.StreamKey, session.StreamUrl);
    }

    [Fact]
    public async Task Update_and_delete_are_owner_gated()
    {
        var (db, service, course) = SeedAsync();
        await service.CreateAsync(course.Id, "owner-1", Input());
        var session = await db.Set<LiveSession>().SingleAsync();

        var (updateNonOwner, _) = await service.UpdateAsync(session.Id, "student-1", Input());
        var (updateOk, updateError) = await service.UpdateAsync(session.Id, "owner-1", Input());
        var deleteByStudent = await service.DeleteAsync(session.Id, "student-1");
        var deleteByOwner = await service.DeleteAsync(session.Id, "owner-1");

        Assert.False(updateNonOwner);
        Assert.True(updateOk);
        Assert.Null(updateError);
        Assert.False(deleteByStudent);
        Assert.True(deleteByOwner);
        Assert.Empty(db.Set<LiveSession>());
    }

    [Fact]
    public async Task Start_and_end_require_owner_or_cohost()
    {
        var (db, service, course) = SeedAsync();
        await service.CreateAsync(course.Id, "owner-1", Input());
        var session = await db.Set<LiveSession>().SingleAsync();

        var startByStudent = await service.StartAsync(session.Id, "student-1");
        Assert.False(startByStudent.Ok);

        var start = await service.StartAsync(session.Id, "owner-1");
        Assert.True(start.Ok);
        Assert.Equal(LiveSessionStatus.Live, (await db.Set<LiveSession>().SingleAsync()).Status);

        var end = await service.EndAsync(session.Id, "owner-1", null);
        Assert.True(end.Ok);
        Assert.Equal(LiveSessionStatus.Ended, (await db.Set<LiveSession>().SingleAsync()).Status);
    }

    [Fact]
    public async Task Cohost_invite_and_remove()
    {
        var (db, service, course) = SeedAsync();
        await service.CreateAsync(course.Id, "owner-1", Input());
        var session = await db.Set<LiveSession>().SingleAsync();

        var invite = await service.AddCoHostAsync(session.Id, "owner-1", "cohost@example.com");
        Assert.True(invite.Ok);
        Assert.True(await service.IsCoHostAsync(session.Id, "cohost-1"));
        Assert.True(await service.CanManageAsync(session.Id, "cohost-1"));

        var duplicate = await service.AddCoHostAsync(session.Id, "owner-1", "cohost@example.com");
        Assert.False(duplicate.Ok);
        Assert.Contains("already", duplicate.Error, StringComparison.OrdinalIgnoreCase);

        var removed = await service.RemoveCoHostAsync(session.Id, "owner-1", "cohost-1");
        Assert.True(removed);
        Assert.False(await service.IsCoHostAsync(session.Id, "cohost-1"));
    }

    [Fact]
    public async Task Check_in_only_once_and_only_while_live()
    {
        var (db, service, course) = SeedAsync();
        var start = DateTime.UtcNow.AddHours(-1);
        var input = new LiveInput("Now", null, start, start.AddHours(2));
        await service.CreateAsync(course.Id, "owner-1", input);
        var session = await db.Set<LiveSession>().SingleAsync();
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "student-1", CourseId = course.Id });
        await db.SaveChangesAsync();
        await service.StartAsync(session.Id, "owner-1");

        var checkIn = await service.CheckInAsync(session.Id, "student-1");
        var second = await service.CheckInAsync(session.Id, "student-1");

        Assert.True(checkIn.Ok);
        Assert.False(second.Ok);
        Assert.Contains("already", second.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(db.Set<LiveCheckIn>());
        Assert.True(await service.HasCheckedInAsync(session.Id, "student-1"));
    }

    [Fact]
    public async Task Check_in_rejected_when_not_live()
    {
        var (db, service, course) = SeedAsync();
        await service.CreateAsync(course.Id, "owner-1", Input());
        var session = await db.Set<LiveSession>().SingleAsync();

        var (ok, error) = await service.CheckInAsync(session.Id, "student-1");

        Assert.False(ok);
        Assert.Contains("live", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Access_requires_enrollment_owner_cohost_or_admin()
    {
        var (db, service, course) = SeedAsync();
        await service.CreateAsync(course.Id, "owner-1", Input());
        var session = await db.Set<LiveSession>().SingleAsync();

        Assert.False(await service.CanAccessAsync(session.Id, "student-1", isAdmin: false));
        Assert.True(await service.CanAccessAsync(session.Id, "owner-1", isAdmin: false));
        Assert.True(await service.CanAccessAsync(session.Id, "nobody", isAdmin: true));

        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "student-1", CourseId = course.Id });
        await db.SaveChangesAsync();
        Assert.True(await service.CanAccessAsync(session.Id, "student-1", isAdmin: false));
    }

    [Fact]
    public async Task Live_chat_persists_and_is_scoped()
    {
        var (db, service, course) = SeedAsync();
        await service.CreateAsync(course.Id, "owner-1", Input());
        var session = await db.Set<LiveSession>().SingleAsync();
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "student-1", CourseId = course.Id });
        await db.SaveChangesAsync();

        var rejected = await service.AddLiveMessageAsync(session.Id, "outsider", "hello");
        var message = await service.AddLiveMessageAsync(session.Id, "student-1", "hello everyone");

        Assert.Null(rejected);
        Assert.NotNull(message);
        Assert.Equal(session.CourseId, message.CourseId);
        Assert.Equal(session.Id, message.SessionId);
        var history = await service.GetLiveMessagesAsync(session.Id);
        Assert.Single(history);
        Assert.Equal("hello everyone", history[0].Body);
        Assert.Contains(db.Set<ChatMessage>(), m => m.SessionId == session.Id);
    }
}
