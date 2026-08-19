using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Services;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;
using OpenLearning.Progress.Models;
using OpenLearning.Progress.Services;
using OpenLearning.Settlement.Models;
using OpenLearning.Settlement.Services;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Jobs;

public sealed class ScheduledJobsTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static OrderService CreateOrders(ApplicationDbContext db)
    {
        var enrollments = new EnrollmentService(db);
        return new OrderService(db, enrollments, new CartService(db, enrollments), new CouponService(db), new LedgerService(db));
    }

    [Fact]
    public async Task ExpireUnpaid_closes_old_orders_and_releases_coupon()
    {
        var db = CreateDb();
        db.Set<Course>().Add(new Course { Id = 1, Title = "C", InstructorId = "i", Status = CourseStatus.Published, Price = 10m });
        var coupon = new Coupon { Code = "A10", DiscountPercent = 10, Uses = 1 };
        db.Set<Coupon>().Add(coupon);
        await db.SaveChangesAsync();
        var oldOrder = new Order { CourseId = 1, StudentId = "s1", Amount = 9m, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow.AddHours(-1), CouponId = coupon.Id };
        var newOrder = new Order { CourseId = 1, StudentId = "s2", Amount = 9m, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow };
        db.Set<Order>().AddRange(oldOrder, newOrder);
        db.Set<CouponRedemption>().Add(new CouponRedemption { CouponId = coupon.Id, UserId = "s1", OrderId = oldOrder.Id });
        await db.SaveChangesAsync();

        var service = CreateOrders(db);
        var closed = await service.ExpireUnpaidAsync(TimeSpan.FromMinutes(30));
        Assert.Single(closed);
        Assert.Equal(OrderStatus.Cancelled, (await db.Set<Order>().FindAsync(oldOrder.Id))!.Status);
        Assert.Empty(db.Set<CouponRedemption>());
        Assert.Equal(0, (await db.Set<Coupon>().FindAsync(coupon.Id))!.Uses);

        // Idempotent second run.
        var second = await service.ExpireUnpaidAsync(TimeSpan.FromMinutes(30));
        Assert.Empty(second);
    }

    [Fact]
    public async Task TimeoutCloseRefunds_rejects_old_requests()
    {
        var db = CreateDb();
        db.Set<Course>().Add(new Course { Id = 1, Title = "C", InstructorId = "i", Status = CourseStatus.Published, Price = 10m });
        await db.SaveChangesAsync();
        var old = new Order { CourseId = 1, StudentId = "s1", Amount = 9m, Status = OrderStatus.Paid, RefundStatus = RefundStatus.Requested, RefundRequestedAt = DateTime.UtcNow.AddDays(-8) };
        var fresh = new Order { CourseId = 1, StudentId = "s2", Amount = 9m, Status = OrderStatus.Paid, RefundStatus = RefundStatus.Requested, RefundRequestedAt = DateTime.UtcNow.AddHours(-1) };
        db.Set<Order>().AddRange(old, fresh);
        await db.SaveChangesAsync();

        var service = CreateOrders(db);
        var rejected = await service.TimeoutCloseRefundsAsync(TimeSpan.FromDays(7));

        var oldRow = Assert.Single(rejected);
        Assert.Equal(RefundStatus.Rejected, oldRow.RefundStatus);
        Assert.Equal(RefundStatus.Requested, (await db.Set<Order>().FindAsync(fresh.Id))!.RefundStatus);

        Assert.Empty(await service.TimeoutCloseRefundsAsync(TimeSpan.FromDays(7)));
    }

    [Fact]
    public async Task CouponDisable_expires_past_coupons()
    {
        var db = CreateDb();
        var expired = new Coupon { Code = "OLD", DiscountPercent = 10, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        var active = new Coupon { Code = "NEW", DiscountPercent = 10, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        db.Set<Coupon>().AddRange(expired, active);
        await db.SaveChangesAsync();

        var service = new CouponService(db);
        var count = await service.DisableExpiredAsync(DateTime.UtcNow);

        Assert.Equal(1, count);
        Assert.False((await db.Set<Coupon>().FindAsync(expired.Id))!.IsActive);
        Assert.True((await db.Set<Coupon>().FindAsync(active.Id))!.IsActive);
        Assert.Equal(0, await service.DisableExpiredAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task SettlementPeriodClose_is_idempotent()
    {
        var db = CreateDb();
        var start = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        db.Set<SettlementLedger>().AddRange(
            new SettlementLedger { InstructorId = "i1", Amount = 100m, CreatedAt = start.AddDays(1), Reason = "sale" },
            new SettlementLedger { InstructorId = "i1", Amount = -20m, CreatedAt = start.AddDays(2), Reason = "refund" },
            new SettlementLedger { InstructorId = "i2", Amount = 50m, CreatedAt = start.AddDays(1), Reason = "sale" });
        await db.SaveChangesAsync();

        var service = new SettlementService(db);
        var created = await service.CloseInstructorPeriodAsync(start, start.AddDays(7));
        var again = await service.CloseInstructorPeriodAsync(start, start.AddDays(7));

        Assert.Equal(2, created);
        Assert.Equal(0, again);
        var statements = await db.Set<SettlementStatement>().ToListAsync();
        Assert.Equal(2, statements.Count);
        var i1 = Assert.Single(statements, s => s.InstructorId == "i1");
        Assert.Equal(80m, i1.NetAmount);
    }

    [Fact]
    public async Task StudyDailyAggregate_upserts_idempotently()
    {
        var db = CreateDb();
        var course = new Course { Title = "C", InstructorId = "i", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        var enrollment = new EnrollmentEntity { StudentId = "s1", CourseId = course.Id };
        db.Set<EnrollmentEntity>().Add(enrollment);
        await db.SaveChangesAsync();

        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.Set<StudySession>().AddRange(
            new StudySession { UserId = "s1", CourseId = course.Id, EnrollmentId = enrollment.Id, StartedAt = start.AddMinutes(10), DurationSeconds = 300 },
            new StudySession { UserId = "s1", CourseId = course.Id, EnrollmentId = enrollment.Id, StartedAt = start.AddMinutes(40), DurationSeconds = 200 });
        db.Set<LessonCompletion>().Add(new LessonCompletion { EnrollmentId = enrollment.Id, LessonId = 1, CompletedAt = start.AddMinutes(20) });
        await db.SaveChangesAsync();

        var service = new StudyToolService(db, new ProgressService(db));
        await service.AggregateDailyAsync(day);
        await service.AggregateDailyAsync(day);

        var aggregates = await db.Set<StudyDailyAggregate>().ToListAsync();
        var row = Assert.Single(aggregates);
        Assert.Equal(500, row.TotalSeconds);
        Assert.Equal(1, row.LessonsCompleted);
    }

    [Fact]
    public async Task Assignment_past_due_submit_blocked_and_due_soon_listed()
    {
        var db = CreateDb();
        var dueSoon = new Assignment { Title = "Soon", CourseId = 1, AuthorId = "i", DueAt = DateTime.UtcNow.AddHours(12) };
        var pastDue = new Assignment { Title = "Past", CourseId = 1, AuthorId = "i", DueAt = DateTime.UtcNow.AddHours(-1) };
        db.Set<Assignment>().AddRange(dueSoon, pastDue);
        await db.SaveChangesAsync();

        var service = new AssignmentService(db);
        var due = await service.ListDueWithinAsync(DateTime.UtcNow, TimeSpan.FromHours(24));
        Assert.Single(due);
        Assert.Equal("Soon", due[0].Title);

        var blocked = await service.SubmitAsync(pastDue.Id, "s1", "late text", null);
        Assert.False(blocked.Ok);
        Assert.Contains("past its due date", blocked.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Exam_and_class_starting_within_queries()
    {
        var db = CreateDb();
        db.Set<Exam>().AddRange(
            new Exam { Title = "Soon", CourseId = 1, AuthorId = "i", OpensAt = DateTime.UtcNow.AddMinutes(15) },
            new Exam { Title = "Later", CourseId = 1, AuthorId = "i", OpensAt = DateTime.UtcNow.AddHours(2) });
        var now = DateTime.UtcNow;
        db.Set<ClassGroup>().AddRange(
            new ClassGroup { Name = "C1", CourseId = 1, StartsAt = now.AddMinutes(20), EndsAt = now.AddHours(2) });
        await db.SaveChangesAsync();

        var exams = new ExamService(db, new EnrollmentService(db), new IncorrectAnswerService(db));
        var examIds = await exams.ListStartingWithinAsync(DateTime.UtcNow, TimeSpan.FromMinutes(30));
        Assert.Single(examIds);
        Assert.False(await exams.HasAttemptedAsync(examIds[0].Id, "s1"));

        var classes = new ClassGroupService(db);
        var classIds = await classes.ListStartingWithinAsync(DateTime.UtcNow, TimeSpan.FromMinutes(30));
        var classGroup = Assert.Single(classIds);
        var member = new EnrollmentEntity { StudentId = "s1", CourseId = 1, ClassGroupId = classGroup.Id };
        db.Set<EnrollmentEntity>().Add(member);
        await db.SaveChangesAsync();
        var members = await classes.ListMemberStudentIdsAsync(classGroup.Id);
        Assert.Equal("s1", Assert.Single(members));
    }
}
