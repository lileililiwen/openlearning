using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Logging.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Web.Pages.Courses.Enrollments;

public class EditModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly LogService _logs;

    public EditModel(CourseService courses, EnrollmentService enrollments, LogService logs)
    {
        _courses = courses;
        _enrollments = enrollments;
        _logs = logs;
    }

    public Course? Course { get; set; }

    public List<EnrollmentEntity> Enrollments { get; set; } = new();

    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Finance);
        if (course.InstructorId != userId && !IsAdmin)
        {
            return Forbid();
        }

        var (enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(id);
        Course = course;
        Enrollments = enrollments;
        return Page();
    }

    public async Task<IActionResult> OnPostSetExpiryAsync(int id, int enrollmentId, DateTime? expiresAt)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Finance);
        var (ok, error) = await _enrollments.SetExpiryAsync(enrollmentId, expiresAt, userId, isAdmin);
        if (ok)
        {
            await _logs.RecordAsync(
                userId,
                User.Identity?.Name ?? string.Empty,
                "SetEnrollmentExpiry",
                "Enrollment",
                enrollmentId.ToString(CultureInfo.InvariantCulture),
                expiresAt?.ToString("o", CultureInfo.InvariantCulture),
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Access period updated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id, int enrollmentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Finance);
        var (ok, error) = await _enrollments.RevokeAsync(enrollmentId, "admin", userId, isAdmin);
        if (ok)
        {
            await _logs.RecordAsync(
                userId,
                User.Identity?.Name ?? string.Empty,
                "RevokeEnrollment",
                "Enrollment",
                enrollmentId.ToString(CultureInfo.InvariantCulture),
                "admin",
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Enrollment revoked." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
