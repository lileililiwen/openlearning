using OpenLearning.Assignments.Services;
using OpenLearning.Classes.Services;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Exams.Services;
using OpenLearning.Jobs;
using OpenLearning.Logging.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Settlement.Services;
using OpenLearning.StudyTools.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Jobs;

/// <summary>
/// Closes unpaid orders older than 30 minutes and releases their coupon holds.
/// Idempotency: only Pending orders are touched, so a re-run finds nothing.
/// </summary>
public sealed class OrderExpireUnpaidJob : IJob
{
    public string Key => "order.expire-unpaid";

    public string Cron => "*/1 * * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(5);

    private static readonly TimeSpan _age = TimeSpan.FromMinutes(30);

    private readonly OrderService _orders;
    private readonly NotificationService _notifications;

    public OrderExpireUnpaidJob(OrderService orders, NotificationService notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var closed = await _orders.ExpireUnpaidAsync(_age);
        foreach (var order in closed)
        {
            await _notifications.SendAsync(
                NotificationService.EventKeys.OrderExpired,
                order.StudentId,
                new Dictionary<string, string>
                {
                    ["courseTitle"] = order.Course?.Title ?? string.Empty,
                    ["amount"] = order.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                },
                $"/Courses/Details?id={order.CourseId}");
        }
    }
}

/// <summary>
/// Rejects refund requests pending longer than 7 days (reason "timeout") and
/// notifies the student. Idempotency: only Requested refunds are touched.
/// </summary>
public sealed class RefundTimeoutCloseJob : IJob
{
    public string Key => "refund.timeout-close";

    public string Cron => "0 3 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(10);

    private static readonly TimeSpan _age = TimeSpan.FromDays(7);

    private readonly OrderService _orders;
    private readonly NotificationService _notifications;

    public RefundTimeoutCloseJob(OrderService orders, NotificationService notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var rejected = await _orders.TimeoutCloseRefundsAsync(_age);
        foreach (var order in rejected)
        {
            await _notifications.SendAsync(
                NotificationService.EventKeys.RefundTimeoutRejected,
                order.StudentId,
                new Dictionary<string, string>
                {
                    ["courseTitle"] = order.Course?.Title ?? string.Empty,
                },
                $"/Orders/Detail?id={order.Id}");
        }
    }
}

/// <summary>
/// Reminds students about assignments due within 24 hours and about
/// past-due assignments they have not submitted. Submissions past the due
/// date are already blocked by the service. Idempotency: notifications are
/// per (assignment, student) with no row state — the job-scheduler idempotency
/// key prevents duplicate ticks within a cycle.
/// </summary>
public sealed class AssignmentDueReminderJob : IJob
{
    public string Key => "assignment.due-reminder";

    public string Cron => "0 * * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(15);

    private readonly AssignmentService _assignments;
    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;

    public AssignmentDueReminderJob(
        AssignmentService assignments,
        EnrollmentService enrollments,
        NotificationService notifications)
    {
        _assignments = assignments;
        _enrollments = enrollments;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueSoon = await _assignments.ListDueWithinAsync(now, TimeSpan.FromHours(24));
        foreach (var assignment in dueSoon)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(assignment.CourseId);
            var studentIds = enrollments.Select(e => e.StudentId).ToList();
            var submissionFlags = await Task.WhenAll(studentIds.Select(async s => new
            {
                Id = s,
                Submitted = await _assignments.GetSubmissionAsync(assignment.Id, s) is not null,
            }));
            var unsubmitted = submissionFlags.Where(f => !f.Submitted).Select(f => f.Id).ToList();

            var days = assignment.DueAt is DateTime due
                ? Math.Max(0, (int)Math.Ceiling((due - now).TotalDays))
                : 0;
            foreach (var studentId in unsubmitted)
            {
                await _notifications.SendAsync(
                    NotificationService.EventKeys.AssignmentDueSoon,
                    studentId,
                    new Dictionary<string, string>
                    {
                        ["assignmentTitle"] = assignment.Title,
                        ["days"] = days.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    },
                    $"/Courses/Assignments/Detail?id={assignment.Id}");
            }
        }

        // Auto-close path: assignments whose due date passed get one due-missed
        // notification per non-submitting enrolled student, then are marked.
        var pastDue = await _assignments.ListPastDueUnnotifiedAsync(now);
        foreach (var assignment in pastDue)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(assignment.CourseId);
            var studentIds = enrollments.Select(e => e.StudentId).ToList();
            var submitterIds = await _assignments.GetSubmittingStudentIdsAsync(assignment.Id);
            var unsubmitted = studentIds.Where(id => !submitterIds.Contains(id)).ToList();

            foreach (var studentId in unsubmitted)
            {
                await _notifications.SendAsync(
                    NotificationService.EventKeys.AssignmentDueMissed,
                    studentId,
                    new Dictionary<string, string> { ["assignmentTitle"] = assignment.Title },
                    $"/Courses/Assignments/Detail?id={assignment.Id}");
            }

            await _assignments.MarkDueMissedNotifiedAsync(assignment.Id);
        }
    }
}

/// <summary>
/// Reminds enrolled students of exams starting within 30 minutes (once per
/// student who has not yet attempted). Idempotency: attempts act as the row
/// state; reminders are re-emitted only for students without an attempt.
/// </summary>
public sealed class ExamReminderJob : IJob
{
    public string Key => "exam.reminder";

    public string Cron => "*/5 * * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(10);

    private readonly ExamService _exams;
    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;

    public ExamReminderJob(ExamService exams, EnrollmentService enrollments, NotificationService notifications)
    {
        _exams = exams;
        _enrollments = enrollments;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var exams = await _exams.ListStartingWithinAsync(now, TimeSpan.FromMinutes(30));
        foreach (var exam in exams)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip exams already reminded in a previous tick.
            if (exam.ReminderNotifiedAt is not null)
            {
                continue;
            }

            var (enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(exam.CourseId);
            var studentIds = enrollments.Select(e => e.StudentId).ToList();
            var attemptFlags = await Task.WhenAll(studentIds.Select(async s => new
            {
                Id = s,
                Attempted = await _exams.HasAttemptedAsync(exam.Id, s),
            }));
            var notAttempted = attemptFlags.Where(f => !f.Attempted).Select(f => f.Id).ToList();

            foreach (var studentId in notAttempted)
            {
                await _notifications.SendAsync(
                    NotificationService.EventKeys.ExamStartingSoon,
                    studentId,
                    new Dictionary<string, string> { ["examTitle"] = exam.Title },
                    $"/Courses/Exams/Take?id={exam.Id}");
            }

            await _exams.MarkReminderNotifiedAsync(exam.Id);
        }
    }
}

/// <summary>
/// Sends a class-scoped reminder to members of class groups starting within
/// 30 minutes. Idempotency: notification per (class, member); the scheduler's
/// idempotency key covers duplicate ticks within the cycle.
/// </summary>
public sealed class ClassStartReminderJob : IJob
{
    public string Key => "class.start-reminder";

    public string Cron => "*/5 * * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(10);

    private readonly ClassGroupService _classes;
    private readonly NotificationService _notifications;

    public ClassStartReminderJob(ClassGroupService classes, NotificationService notifications)
    {
        _classes = classes;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var classes = await _classes.ListStartingWithinAsync(now, TimeSpan.FromMinutes(30));
        foreach (var classGroup in classes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _notifications.SendClassScopedAsync(
                NotificationService.EventKeys.ClassStartingSoon,
                classGroup.Id,
                new Dictionary<string, string> { ["className"] = classGroup.Name },
                $"/Courses/Classes/Detail?id={classGroup.Id}");
        }
    }
}

/// <summary>
/// Aggregates the previous UTC day's StudySession rows into per-day,
/// per-student, per-course summaries. Idempotent: rows are upserted.
/// </summary>
public sealed class StudyDailyAggregateJob : IJob
{
    public string Key => "study.daily-aggregate";

    public string Cron => "0 3 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly StudyToolService _studyTools;

    public StudyDailyAggregateJob(StudyToolService studyTools)
    {
        _studyTools = studyTools;
    }

    public Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        return _studyTools.AggregateDailyAsync(yesterday);
    }
}

/// <summary>
/// Freezes each instructor's weekly earnings into a SettlementStatement.
/// Idempotent: instructors with a statement for the same period are skipped.
/// </summary>
public sealed class InstructorSettlementCloseJob : IJob
{
    public string Key => "settlement.instructor-period-close";

    public string Cron => "0 23 * * 0";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly SettlementService _settlement;

    public InstructorSettlementCloseJob(SettlementService settlement)
    {
        _settlement = settlement;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var start = StartOfWeek(now);
        var end = start.AddDays(7);
        await _settlement.CloseInstructorPeriodAsync(start, end);
    }

    private static DateTime StartOfWeek(DateTime utcNow)
    {
        var daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7;
        return utcNow.Date.AddDays(-daysSinceMonday);
    }
}

/// <summary>
/// Deactivates coupons whose EndsAt has passed. Idempotent: only active
/// expired coupons are touched.
/// </summary>
public sealed class CouponExpireDisabledJob : IJob
{
    public string Key => "coupon.expire-disabled";

    public string Cron => "0 * * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(5);

    private readonly CouponService _coupons;

    public CouponExpireDisabledJob(CouponService coupons)
    {
        _coupons = coupons;
    }

    public Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        return _coupons.DisableExpiredAsync(DateTime.UtcNow);
    }
}

/// <summary>
/// Prunes operation/error log rows older than the configured retention
/// (system-config `logging.retention.days`, default 90). Idempotent.
/// </summary>
public sealed class LogArchiveJob : IJob
{
    public string Key => "logs.archive";

    public string Cron => "0 5 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly LogService _logs;
    private readonly SystemConfigService _config;

    public LogArchiveJob(LogService logs, SystemConfigService config)
    {
        _logs = logs;
        _config = config;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var retentionDays = Math.Clamp(await _config.GetIntAsync("logging.retention.days", 90), 1, 3650);
        await _logs.PruneAsync(retentionDays);
    }
}
