using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.SystemConfig.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Web.Pages;

[Authorize(Policy = Policies.RequireStudent)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "我的课程")]
public class MyCoursesModel : PageModel
{
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly SystemConfigService _config;

    public MyCoursesModel(EnrollmentService enrollments, ProgressService progress, SystemConfigService config)
    {
        _enrollments = enrollments;
        _progress = progress;
        _config = config;
    }

    public record EnrolledCourse(
        EnrollmentEntity Enrollment,
        int ProgressPercent,
        bool IsExpired,
        bool IsRevoked,
        int DaysRemaining);

    public List<EnrolledCourse> Courses { get; set; } = new();

    public int GraceDays { get; set; } = 3;

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        GraceDays = Math.Clamp(await _config.GetIntAsync("enrollment.expiry.graceDays", 3), 0, 365);
        var enrollments = await _enrollments.GetStudentEnrollmentsAsync(userId);
        var now = DateTime.UtcNow;

        var courses = new List<EnrolledCourse>();
        foreach (var enrollment in enrollments)
        {
            var percent = await _progress.GetProgressPercentAsync(userId, enrollment.CourseId);
            var expired = enrollment.AccessExpiresAt is DateTime deadline && now > deadline;
            var daysRemaining = enrollment.AccessExpiresAt is DateTime expiry
                ? (int)Math.Ceiling((expiry - now).TotalDays)
                : 0;
            courses.Add(new EnrolledCourse(
                enrollment,
                percent,
                expired,
                enrollment.RevokedAt is not null,
                daysRemaining));
        }

        Courses = courses;
    }

    public async Task<IActionResult> OnPostWithdrawAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _enrollments.WithdrawAsync(userId, id);
        return RedirectToPage();
    }
}
