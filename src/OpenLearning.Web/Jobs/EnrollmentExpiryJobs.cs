using OpenLearning.Enrollment.Services;
using OpenLearning.Jobs;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Jobs;

/// <summary>
/// Revokes enrollments whose access expired beyond the grace period and
/// notifies the learner. Cron registration is delegated to the
/// scheduled-business-jobs change.
/// </summary>
public sealed class EnrollmentExpiryRevokeJob : IJob
{
    public string Key => "enrollment.expiry.revoke";

    public string Cron => "0 2 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;
    private readonly SystemConfigService _config;

    public EnrollmentExpiryRevokeJob(
        EnrollmentService enrollments,
        NotificationService notifications,
        SystemConfigService config)
    {
        _enrollments = enrollments;
        _notifications = notifications;
        _config = config;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var graceDays = Math.Clamp(await _config.GetIntAsync("enrollment.expiry.graceDays", 3), 0, 365);
        var expired = await _enrollments.ListExpiredPastGraceAsync(graceDays);
        foreach (var enrollment in expired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _enrollments.RevokeAsync(enrollment.Id, "expired", "scheduler", isAdminOrFinance: true);
            await _notifications.CreateAsync(
                enrollment.StudentId,
                NotificationType.Enrollment,
                "Course access expired",
                $"Your access to {enrollment.Course?.Title ?? "a course"} has ended. Re-enroll to continue learning.",
                $"/Courses/Details?id={enrollment.CourseId}");
        }
    }
}

/// <summary>
/// Notifies learners whose enrollment access expires within the next 7 days.
/// Cron registration is delegated to the scheduled-business-jobs change.
/// </summary>
public sealed class EnrollmentExpiryNotifySoonJob : IJob
{
    public string Key => "enrollment.expiry.notify-soon";

    public string Cron => "0 1 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(15);

    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;

    public EnrollmentExpiryNotifySoonJob(EnrollmentService enrollments, NotificationService notifications)
    {
        _enrollments = enrollments;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var expiring = await _enrollments.ListExpiringWithinAsync(7);
        foreach (var enrollment in expiring)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var days = enrollment.AccessExpiresAt is DateTime deadline
                ? Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalDays))
                : 0;
            await _notifications.CreateAsync(
                enrollment.StudentId,
                NotificationType.Enrollment,
                "Course access expiring soon",
                $"Your access to {enrollment.Course?.Title ?? "a course"} ends in {days} day(s). Please renew in time.",
                $"/Courses/Details?id={enrollment.CourseId}");
        }
    }
}
