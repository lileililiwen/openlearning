using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Logging.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Web.Pages.Courses.Roster;

/// <summary>One row of the teacher roster.</summary>
public sealed record RosterRow(
    int EnrollmentId,
    string StudentId,
    string StudentName,
    string StudentEmail,
    DateTime EnrolledAt,
    int ProgressPercent,
    int CompletedLessons,
    int TotalLessons,
    DateTime? LastAccessedAt,
    int StudyDurationSeconds);

[Authorize(Policy = Policies.RequireInstructor)]
public class IndexModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly LogService _logs;

    public IndexModel(
        CourseService courses,
        EnrollmentService enrollments,
        ProgressService progress,
        LogService logs)
    {
        _courses = courses;
        _enrollments = enrollments;
        _progress = progress;
        _logs = logs;
    }

    public static string FormatDuration(int seconds)
    {
        var totalMinutes = (int)Math.Ceiling(seconds / 60.0);
        if (totalMinutes < 60)
        {
            return $"{totalMinutes} min";
        }

        return $"{(totalMinutes / 60)} h {totalMinutes % 60} min";
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public List<RosterRow> Roster { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _courses.IsOwnerAsync(id, userId))
        {
            return Forbid();
        }

        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        CourseId = id;
        CourseTitle = course.Title;

        var (enrollments, totalLessons) = await _enrollments.GetEnrollmentsForRosterAsync(id);
        if (enrollments.Count == 0)
        {
            Roster = new List<RosterRow>();
            return Page();
        }

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var (completedByEnrollment, lastAccessByEnrollment) = await _progress.GetEnrollmentProgressMapAsync(enrollmentIds);
        var durationByEnrollment = await _progress.GetDurationByEnrollmentAsync(enrollmentIds);

        Roster = enrollments.Select(e => new RosterRow(
            e.Id,
            e.StudentId,
            e.Student?.DisplayName ?? string.Empty,
            e.Student?.Email ?? string.Empty,
            e.EnrolledAt,
            totalLessons == 0
                ? 0
                : (int)Math.Round((completedByEnrollment.GetValueOrDefault(e.Id) * 100.0) / totalLessons),
            completedByEnrollment.GetValueOrDefault(e.Id),
            totalLessons,
            lastAccessByEnrollment.TryGetValue(e.Id, out var last) ? (DateTime?)last : null,
            durationByEnrollment.GetValueOrDefault(e.Id)))
            .Where(r => string.IsNullOrWhiteSpace(Search)
                || r.StudentName.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || r.StudentEmail.Contains(Search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.StudentName)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostWithdrawAsync(int id, string studentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _courses.IsOwnerAsync(id, userId))
        {
            return Forbid();
        }

        var ok = await _enrollments.WithdrawAsync(studentId, id);
        if (ok)
        {
            await _logs.RecordAsync(
                userId,
                User.Identity?.Name ?? string.Empty,
                "WithdrawStudent",
                "Course",
                id.ToString(CultureInfo.InvariantCulture),
                $"student {studentId}",
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Student withdrawn." : "Could not withdraw the student.";
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
