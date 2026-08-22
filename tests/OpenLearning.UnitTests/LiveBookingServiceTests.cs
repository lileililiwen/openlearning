using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests;

public class LiveBookingServiceTests
{
    private static (ApplicationDbContext Db, LiveBookingService Service) Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var service = new LiveBookingService(db);
        return (db, service);
    }

    private static async Task<(ApplicationDbContext Db, LiveBookingService Service, int SessionId)>
        SeedSessionAsync(
            int capacity = 2,
            bool bookingEnabled = true,
            DateTime? opensAt = null,
            DateTime? closesAt = null,
            DateTime? deadline = null)
    {
        var (db, service) = Create();
        var course = new Course { Id = 1, InstructorId = "instructor1", Title = "Test Course" };
        var session = new LiveSession
        {
            Id = 1,
            CourseId = 1,
            InstructorId = "instructor1",
            Title = "Live Session",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            StreamKey = "key",
            StreamUrl = "url",
            IsBookingEnabled = bookingEnabled,
            Capacity = capacity,
            BookingOpensAt = opensAt,
            BookingClosesAt = closesAt,
            CancellationDeadline = deadline,
        };
        db.Set<Course>().Add(course);
        db.Set<LiveSession>().Add(session);
        await db.SaveChangesAsync();
        return (db, service, 1);
    }

    private static async Task SeedEnrollmentAsync(ApplicationDbContext db, string studentId, int courseId = 1)
    {
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = studentId, CourseId = courseId });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Reserve_confirmed_when_capacity_available()
    {
        var (db, service, sessionId) = await SeedSessionAsync(capacity: 3);
        await SeedEnrollmentAsync(db, "student1");

        var (ok, error, position) = await service.ReserveAsync(sessionId, "student1");

        Assert.True(ok);
        Assert.Null(error);
        Assert.Null(position);
        Assert.Equal(1, await db.Set<LiveBooking>().CountAsync(b => b.SessionId == sessionId && b.Status == LiveBookingStatus.Confirmed));
    }

    [Fact]
    public async Task Reserve_waitlisted_when_capacity_full()
    {
        var (db, service, sessionId) = await SeedSessionAsync(capacity: 1);
        await SeedEnrollmentAsync(db, "student1");
        await SeedEnrollmentAsync(db, "student2");

        var (ok1, _, _) = await service.ReserveAsync(sessionId, "student1");
        Assert.True(ok1);

        var (ok2, error2, position) = await service.ReserveAsync(sessionId, "student2");

        Assert.True(ok2);
        Assert.Null(error2);
        Assert.Equal(1, position);
        Assert.Equal(1, await db.Set<LiveWaitlist>().CountAsync(w => w.SessionId == sessionId));
    }

    [Fact]
    public async Task Cancel_promotes_first_waitlisted()
    {
        var (db, service, sessionId) = await SeedSessionAsync(capacity: 1);
        await SeedEnrollmentAsync(db, "student1");
        await SeedEnrollmentAsync(db, "student2");

        await service.ReserveAsync(sessionId, "student1");
        await service.ReserveAsync(sessionId, "student2");

        var (ok, _) = await service.CancelAsync(sessionId, "student1");

        Assert.True(ok);
        Assert.Equal(1, await db.Set<LiveBooking>().CountAsync(b =>
            b.SessionId == sessionId && b.Status == LiveBookingStatus.Confirmed && b.StudentId == "student2"));
        Assert.NotNull(await db.Set<LiveWaitlist>().FirstOrDefaultAsync(w =>
            w.SessionId == sessionId && w.StudentId == "student2" && w.PromotedAt != null));
    }

    [Fact]
    public async Task Cancel_skips_ineligible_and_promotes_next()
    {
        var (db, service, sessionId) = await SeedSessionAsync(capacity: 1);
        await SeedEnrollmentAsync(db, "student1");
        await SeedEnrollmentAsync(db, "student2");
        await SeedEnrollmentAsync(db, "student3");

        await service.ReserveAsync(sessionId, "student1");
        await service.ReserveAsync(sessionId, "student2");
        await service.ReserveAsync(sessionId, "student3");

        var enrollment = await db.Set<EnrollmentEntity>().FirstAsync(e => e.StudentId == "student2" && e.CourseId == 1);
        enrollment.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var (ok, _) = await service.CancelAsync(sessionId, "student1");

        Assert.True(ok);
        Assert.NotNull(await db.Set<LiveBooking>().FirstOrDefaultAsync(b =>
            b.SessionId == sessionId && b.StudentId == "student3" && b.Status == LiveBookingStatus.Confirmed));
        Assert.Null(await db.Set<LiveBooking>().FirstOrDefaultAsync(b =>
            b.SessionId == sessionId && b.StudentId == "student2" && b.Status == LiveBookingStatus.Confirmed));
    }

    [Fact]
    public async Task Cancel_after_deadline_is_rejected()
    {
        var deadline = DateTime.UtcNow.AddHours(-1);
        var (db, service, sessionId) = await SeedSessionAsync(deadline: deadline);
        await SeedEnrollmentAsync(db, "student1");
        await service.ReserveAsync(sessionId, "student1");

        var (ok, error) = await service.CancelAsync(sessionId, "student1");

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Booking_outside_window_is_rejected()
    {
        var opensAt = DateTime.UtcNow.AddHours(2);
        var (db, service, sessionId) = await SeedSessionAsync(opensAt: opensAt);
        await SeedEnrollmentAsync(db, "student1");

        var (ok, error, _) = await service.ReserveAsync(sessionId, "student1");

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Double_reserve_is_rejected()
    {
        var (db, service, sessionId) = await SeedSessionAsync(capacity: 5);
        await SeedEnrollmentAsync(db, "student1");

        var (ok1, _, _) = await service.ReserveAsync(sessionId, "student1");
        Assert.True(ok1);

        var (ok2, error2, _) = await service.ReserveAsync(sessionId, "student1");

        Assert.False(ok2);
        Assert.NotNull(error2);
    }

    [Fact]
    public async Task Calendar_feed_with_valid_token_returns_entries()
    {
        var (db, service, sessionId) = await SeedSessionAsync();
        await SeedEnrollmentAsync(db, "student1");
        await service.ReserveAsync(sessionId, "student1");

        var (rawToken, _) = await service.CreateCalendarTokenAsync("student1");

        var entries = await service.GetFeedByTokenAsync(rawToken, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));

        Assert.Single(entries);
        Assert.Equal("Live Session", entries[0].Summary);
    }

    [Fact]
    public async Task Calendar_feed_with_revoked_token_returns_empty()
    {
        var (db, service, sessionId) = await SeedSessionAsync();
        await SeedEnrollmentAsync(db, "student1");
        await service.ReserveAsync(sessionId, "student1");

        var (rawToken, tokenId) = await service.CreateCalendarTokenAsync("student1");
        await service.RevokeCalendarTokenAsync(tokenId, "student1");

        var entries = await service.GetFeedByTokenAsync(rawToken, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));

        Assert.Empty(entries);
    }
}
