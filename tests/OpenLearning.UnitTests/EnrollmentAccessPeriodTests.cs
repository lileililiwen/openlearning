using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Enrollment;

public sealed class EnrollmentAccessPeriodTests
{
    private static (ApplicationDbContext Db, EnrollmentService Service, Course Course) SeedAsync(bool published = true)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course
        {
            Title = "C",
            InstructorId = "instructor-1",
            Status = published ? CourseStatus.Published : CourseStatus.Draft,
        };
        db.Set<Course>().Add(course);
        db.SaveChanges();
        return (db, new EnrollmentService(db), course);
    }

    [Fact]
    public async Task Enroll_without_course_default_has_no_expiry()
    {
        var (db, service, course) = SeedAsync();

        var (ok, _) = await service.EnrollAsync("student-1", course.Id);

        Assert.True(ok);
        Assert.Null(Assert.Single(db.Set<EnrollmentEntity>()).AccessExpiresAt);
    }

    [Fact]
    public async Task Enroll_with_course_default_seeds_expiry()
    {
        var (db, service, course) = SeedAsync();
        course.DefaultAccessDays = 180;
        await db.SaveChangesAsync();

        var (ok, _) = await service.EnrollAsync("student-1", course.Id);

        Assert.True(ok);
        var enrollment = Assert.Single(db.Set<EnrollmentEntity>());
        Assert.NotNull(enrollment.AccessExpiresAt);
        Assert.True(enrollment.AccessExpiresAt > DateTime.UtcNow.AddDays(179));
        Assert.True(enrollment.AccessExpiresAt < DateTime.UtcNow.AddDays(181));
    }

    [Fact]
    public async Task Enroll_with_membership_uses_min_of_membership_and_course_default()
    {
        var (db, service, course) = SeedAsync();
        course.DefaultAccessDays = 180;
        await db.SaveChangesAsync();
        var membershipExpiry = DateTime.UtcNow.AddDays(30);

        var (ok, _) = await service.EnrollAsync("student-1", course.Id, membershipExpiry);

        Assert.True(ok);
        var enrollment = Assert.Single(db.Set<EnrollmentEntity>());
        Assert.True(enrollment.AccessExpiresAt > membershipExpiry.AddDays(-1));
        Assert.True(enrollment.AccessExpiresAt <= membershipExpiry);
    }

    [Fact]
    public async Task Duplicate_enrollment_rejected_until_previous_row_is_revoked()
    {
        var (db, service, course) = SeedAsync();
        await service.EnrollAsync("student-1", course.Id);
        var first = await db.Set<EnrollmentEntity>().SingleAsync();

        var duplicate = await service.EnrollAsync("student-1", course.Id);
        Assert.False(duplicate.Ok);
        Assert.Contains("already enrolled", duplicate.Error, StringComparison.OrdinalIgnoreCase);

        await service.RevokeAsync(first.Id, "expired", "scheduler", isAdminOrFinance: true);
        var reEnroll = await service.EnrollAsync("student-1", course.Id);
        Assert.True(reEnroll.Ok);
        Assert.Equal(2, await db.Set<EnrollmentEntity>().CountAsync());
    }

    [Fact]
    public async Task Revoked_enrollment_does_not_grant_access()
    {
        var (db, service, course) = SeedAsync();
        await service.EnrollAsync("student-1", course.Id);
        var enrollment = await db.Set<EnrollmentEntity>().SingleAsync();
        await service.RevokeAsync(enrollment.Id, "refund", "admin-1", isAdminOrFinance: true);

        Assert.False(await service.IsEnrolledAsync("student-1", course.Id));
        var revoked = await db.Set<EnrollmentEntity>().SingleAsync();
        Assert.NotNull(revoked.RevokedAt);
        Assert.Equal("refund", revoked.RevokedReason);
    }

    [Fact]
    public async Task Revoke_rejects_already_revoked()
    {
        var (db, service, course) = SeedAsync();
        await service.EnrollAsync("student-1", course.Id);
        var enrollment = await db.Set<EnrollmentEntity>().SingleAsync();
        await service.RevokeAsync(enrollment.Id, "expired", "scheduler", isAdminOrFinance: true);

        var second = await service.RevokeAsync(enrollment.Id, "admin", "admin-1", isAdminOrFinance: true);

        Assert.False(second.Ok);
        Assert.Contains("already", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetExpiry_requires_owner_or_admin()
    {
        var (db, service, course) = SeedAsync();
        await service.EnrollAsync("student-1", course.Id);
        var enrollment = await db.Set<EnrollmentEntity>().SingleAsync();
        var future = DateTime.UtcNow.AddDays(60);

        var denied = await service.SetExpiryAsync(enrollment.Id, future, "other-instructor", isAdminOrFinance: false);
        var allowed = await service.SetExpiryAsync(enrollment.Id, future, "instructor-1", isAdminOrFinance: false);

        Assert.False(denied.Ok);
        Assert.True(allowed.Ok);
        Assert.NotNull((await db.Set<EnrollmentEntity>().SingleAsync()).AccessExpiresAt);
    }

    [Fact]
    public async Task IsAccessExpired_evaluates_access_expiry()
    {
        var (db, service, course) = SeedAsync();
        await service.EnrollAsync("student-1", course.Id);
        var enrollment = await db.Set<EnrollmentEntity>().SingleAsync();
        enrollment.AccessExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        Assert.True(await service.IsAccessExpiredAsync("student-1", course.Id));
        Assert.False(await service.IsAccessExpiredAsync("someone-else", course.Id));
    }

    [Fact]
    public async Task ListExpiredPastGrace_and_ListExpiringWithin()
    {
        var (db, service, course) = SeedAsync();
        await service.EnrollAsync("student-1", course.Id);
        await service.EnrollAsync("student-2", course.Id);
        await service.EnrollAsync("student-3", course.Id);
        var rows = await db.Set<EnrollmentEntity>().ToListAsync();
        foreach (var row in rows)
        {
            if (row.StudentId == "student-1")
            {
                row.AccessExpiresAt = DateTime.UtcNow.AddDays(-10);
            }
            else if (row.StudentId == "student-2")
            {
                row.AccessExpiresAt = DateTime.UtcNow.AddDays(3);
            }
            else
            {
                row.AccessExpiresAt = DateTime.UtcNow.AddDays(30);
            }
        }

        await db.SaveChangesAsync();

        var allRows = await db.Set<EnrollmentEntity>().AsNoTracking().ToListAsync();
        Assert.Equal(3, allRows.Count);
        Assert.True(allRows.All(r => r.AccessExpiresAt is not null));

        var expired = await service.ListExpiredPastGraceAsync(graceDays: 5);
        var expiring = await service.ListExpiringWithinAsync(7);

        var expiredRow = Assert.Single(expired);
        Assert.Equal("student-1", expiredRow.StudentId);
        var expiringRow = Assert.Single(expiring);
        Assert.Equal("student-2", expiringRow.StudentId);
    }
}
