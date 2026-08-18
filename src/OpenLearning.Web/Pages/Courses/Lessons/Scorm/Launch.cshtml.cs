using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Scorm.Models;
using OpenLearning.Scorm.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons.Scorm;

public class LaunchModel : PageModel
{
    private readonly LessonService _lessons;
    private readonly EnrollmentService _enrollments;
    private readonly ScormService _scorm;

    public LaunchModel(LessonService lessons, EnrollmentService enrollments, ScormService scorm)
    {
        _lessons = lessons;
        _enrollments = enrollments;
        _scorm = scorm;
    }

    public Lesson? Lesson { get; set; }

    public ScormPackage? Package { get; set; }

    public string ScormUrl { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson?.Module?.Course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var course = lesson.Module.Course;
        var isOwner = userId is not null && course.InstructorId == userId;
        var isAdmin = User.IsInRole(Roles.Admin);

        if (course.Status == CourseStatus.Draft && !isOwner && !isAdmin)
        {
            return Forbid();
        }

        var isEnrolled = userId is not null && await _enrollments.IsEnrolledAsync(userId, course.Id);
        if (course.IsPublished && !isOwner && !isAdmin && !isEnrolled)
        {
            return Forbid();
        }

        var package = await _scorm.GetForLessonAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        Lesson = lesson;
        Package = package;
        ScormUrl = $"/{package.PackagePath}/{package.EntryPoint}";
        if (userId is not null)
        {
            StudentId = userId;
            StudentName = User.Identity?.Name ?? string.Empty;
        }

        return Page();
    }
}
