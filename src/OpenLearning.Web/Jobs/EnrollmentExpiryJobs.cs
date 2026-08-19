using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Jobs;
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
    private readonly CourseService _courses;
    private readonly NotificationService _notifications;
    private readonly SystemConfigService _config;

    public EnrollmentExpiryRevokeJob(
        EnrollmentService enrollments,
        CourseService courses,
        NotificationService notifications,
        SystemConfigService config)
    {
        _enrollments = enrollments;
        _courses = courses;
        _notifications = notifications;
        _config = config;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var graceDays = Math.Clamp(await _config.GetIntAsync("enrollment.expiry.graceDays", 3), 0, 365);
        var expired = await _enrollments.ListExpiredPastGraceAsync(graceDays);
        var titles = await ResolveCourseTitlesAsync(expired.Select(e => e.CourseId));
        foreach (var enrollment in expired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _enrollments.RevokeAsync(enrollment.Id, "expired", "scheduler", isAdminOrFinance: true);
            await _notifications.SendAsync(
                NotificationService.EventKeys.EnrollmentExpired,
                enrollment.StudentId,
                new Dictionary<string, string> { ["courseTitle"] = titles.GetValueOrDefault(enrollment.CourseId) ?? string.Empty },
                $"/Courses/Details?id={enrollment.CourseId}");
        }
    }

    private async Task<Dictionary<int, string>> ResolveCourseTitlesAsync(IEnumerable<int> courseIds)
    {
        var ids = courseIds.Distinct().ToList();
        var courses = await _courses.GetAllAsync();
        return courses.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Title);
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
    private readonly CourseService _courses;
    private readonly NotificationService _notifications;

    public EnrollmentExpiryNotifySoonJob(
        EnrollmentService enrollments,
        CourseService courses,
        NotificationService notifications)
    {
        _enrollments = enrollments;
        _courses = courses;
        _notifications = notifications;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var expiring = await _enrollments.ListExpiringWithinAsync(7);
        var titles = await ResolveCourseTitlesAsync(expiring.Select(e => e.CourseId));
        foreach (var enrollment in expiring)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var days = enrollment.AccessExpiresAt is DateTime deadline
                ? Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalDays))
                : 0;
            await _notifications.SendAsync(
                NotificationService.EventKeys.EnrollmentExpiringSoon,
                enrollment.StudentId,
                new Dictionary<string, string>
                {
                    ["courseTitle"] = titles.GetValueOrDefault(enrollment.CourseId) ?? string.Empty,
                    ["days"] = days.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                $"/Courses/Details?id={enrollment.CourseId}");
        }
    }

    private async Task<Dictionary<int, string>> ResolveCourseTitlesAsync(IEnumerable<int> courseIds)
    {
        var ids = courseIds.Distinct().ToList();
        var courses = await _courses.GetAllAsync();
        return courses.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Title);
    }
}
