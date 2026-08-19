using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Services;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Certificates.Models;
using OpenLearning.Certificates.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Memberships.Models;
using OpenLearning.Memberships.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Web.Pages.Dashboard;

/// <summary>Per-course learning summary shown on the student dashboard.</summary>
public sealed record EnrolledCourseItem(
    int CourseId,
    string CourseTitle,
    string Category,
    bool IsFree,
    DateTime EnrolledAt,
    int ProgressPercent,
    int CompletedLessons,
    int TotalLessons,
    int TotalQuizzes,
    int AttemptedQuizzes,
    string InstructorName);

[Authorize(Policy = Policies.RequireStudent)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "学习中心")]
public class IndexModel : PageModel
{
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly AttemptService _attempts;
    private readonly CourseService _courses;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CertificateService _certificates;
    private readonly NotificationService _notifications;
    private readonly MembershipService _memberships;
    private readonly AssignmentService _assignments;
    private readonly DbContext _db;

    public IndexModel(
        EnrollmentService enrollments,
        ProgressService progress,
        AttemptService attempts,
        CourseService courses,
        UserManager<ApplicationUser> userManager,
        CertificateService certificates,
        NotificationService notifications,
        MembershipService memberships,
        AssignmentService assignments,
        DbContext db)
    {
        _enrollments = enrollments;
        _progress = progress;
        _attempts = attempts;
        _courses = courses;
        _userManager = userManager;
        _certificates = certificates;
        _notifications = notifications;
        _memberships = memberships;
        _assignments = assignments;
        _db = db;
    }

    public string DisplayName { get; set; } = string.Empty;

    public List<EnrolledCourseItem> CourseItems { get; set; } = new();

    public List<ContinueLearningItem> ContinueLearning { get; set; } = new();

    public List<Course> Recommendations { get; set; } = new();

    public List<Certificate> Certificates { get; set; } = new();

    public Membership? ActiveMembership { get; set; }

    /// <summary>Assignments for enrolled courses that are due but not yet graded/submitted.</summary>
    public int AssignmentsDue { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.GetUserAsync(User);
        DisplayName = user?.DisplayName ?? User.Identity?.Name ?? string.Empty;

        var enrollments = await _enrollments.GetStudentEnrollmentsAsync(userId);
        await LoadAssignmentsDueAsync(userId, enrollments.Select(e => e.CourseId).ToList());
        var earnedCourseIds = await _certificates.GetEarnedCourseIdsAsync(userId);
        foreach (var enrollment in enrollments)
        {
            var course = enrollment.Course!;
            var totalLessons = await _courses.GetLessonCountAsync(course.Id);
            var completed = await _progress.GetCompletedLessonIdsAsync(userId, course.Id);
            var (totalQuizzes, attemptedQuizzes) = await _attempts.GetQuizStatusAsync(userId, course.Id);
            var percent = totalLessons > 0 ? (int)Math.Round(completed.Count * 100.0 / totalLessons) : 0;

            CourseItems.Add(new EnrolledCourseItem(
                course.Id,
                course.Title,
                course.Category,
                course.IsFree,
                enrollment.EnrolledAt,
                percent,
                completed.Count,
                totalLessons,
                totalQuizzes,
                attemptedQuizzes,
                course.Instructor?.DisplayName ?? string.Empty));

            // Issuance is idempotent; completed courses get a certificate now.
            var hadCertificate = earnedCourseIds.Contains(course.Id);
            var certificate = await _certificates.EnsureIssuedAsync(userId, course.Id);
            if (certificate is not null && !hadCertificate)
            {
                await _notifications.CreateAsync(
                    userId,
                    NotificationType.Certificate,
                    $"Certificate earned: {course.Title}",
                    "Congratulations! View and print your certificate.",
                    $"/Certificates/View?id={certificate.Id}",
                    new Dictionary<string, string> { ["CourseTitle"] = course.Title });
            }
        }

        Certificates = await _certificates.GetForUserAsync(userId);
        ContinueLearning = await _progress.GetContinueLearningItemsAsync(userId);

        ActiveMembership = await _memberships.GetActiveAsync(userId);
        await SendMembershipRemindersAsync(userId);

        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
        var categories = enrollments
            .Select(e => e.Course!.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();
        Recommendations = await _courses.GetRecommendationsAsync(categories, enrolledCourseIds, 6);
    }

    /// <summary>Counts assignments in enrolled courses that are due and not yet submitted.</summary>
    private async Task LoadAssignmentsDueAsync(string userId, List<int> courseIds)
    {
        if (courseIds.Count == 0)
        {
            return;
        }

        var assignments = await _db.Set<Assignment>()
            .Where(a => courseIds.Contains(a.CourseId))
            .ToListAsync();
        var due = new List<Assignment>();
        foreach (var assignment in assignments)
        {
            if (assignment.DueAt is not null && assignment.DueAt < DateTime.UtcNow)
            {
                continue; // past due no longer counts as "due"
            }

            var submission = await _assignments.GetSubmissionAsync(assignment.Id, userId);
            if (submission is null)
            {
                due.Add(assignment);
            }
        }

        AssignmentsDue = due.Count;
    }

    /// <summary>
    /// One reminder per membership expiring within 7 days. Deduplication uses
    /// the notification link as the stable key (a "Membership" type does not
    /// exist), so repeated dashboard loads only notify once.
    /// </summary>
    private async Task SendMembershipRemindersAsync(string userId)
    {
        var expiring = await _memberships.GetExpiringAsync(withinDays: 7);
        foreach (var membership in expiring.Where(m => m.UserId == userId))
        {
            var link = $"/Memberships/Index?membershipId={membership.Id}";
            var alreadyNotified = await _db.Set<Notification>()
                .Where(n => n.UserId == userId && n.Link == link)
                .AnyAsync();
            if (alreadyNotified)
            {
                continue;
            }

            await _notifications.CreateAsync(
                userId,
                NotificationType.Membership,
                "Membership expiring soon",
                $"Your {membership.Plan?.Name ?? "membership"} expires on " +
                $"{membership.ExpiresAt.ToLocalTime():yyyy-MM-dd}. Renew to keep your benefits.",
                link);
        }
    }
}
