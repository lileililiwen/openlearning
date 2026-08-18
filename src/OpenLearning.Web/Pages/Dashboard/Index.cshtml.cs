using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
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
public class IndexModel : PageModel
{
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly AttemptService _attempts;
    private readonly CourseService _courses;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        EnrollmentService enrollments,
        ProgressService progress,
        AttemptService attempts,
        CourseService courses,
        UserManager<ApplicationUser> userManager)
    {
        _enrollments = enrollments;
        _progress = progress;
        _attempts = attempts;
        _courses = courses;
        _userManager = userManager;
    }

    public string DisplayName { get; set; } = string.Empty;

    public List<EnrolledCourseItem> CourseItems { get; set; } = new();

    public List<ContinueLearningItem> ContinueLearning { get; set; } = new();

    public List<Course> Recommendations { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.GetUserAsync(User);
        DisplayName = user?.DisplayName ?? User.Identity?.Name ?? string.Empty;

        var enrollments = await _enrollments.GetStudentEnrollmentsAsync(userId);
        foreach (var enrollment in enrollments)
        {
            var course = enrollment.Course!;
            var totalLessons = await _courses.GetLessonCountAsync(course.Id);
            var completed = await _progress.GetCompletedLessonIdsAsync(userId, course.Id);
            var (totalQuizzes, attemptedQuizzes) = await _attempts.GetQuizStatusAsync(userId, course.Id);

            CourseItems.Add(new EnrolledCourseItem(
                course.Id,
                course.Title,
                course.Category,
                course.IsFree,
                enrollment.EnrolledAt,
                totalLessons > 0 ? (int)Math.Round(completed.Count * 100.0 / totalLessons) : 0,
                completed.Count,
                totalLessons,
                totalQuizzes,
                attemptedQuizzes,
                course.Instructor?.DisplayName ?? string.Empty));
        }

        ContinueLearning = await _progress.GetContinueLearningItemsAsync(userId);

        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
        var categories = enrollments
            .Select(e => e.Course!.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();
        Recommendations = await _courses.GetRecommendationsAsync(categories, enrolledCourseIds, 6);
    }
}
