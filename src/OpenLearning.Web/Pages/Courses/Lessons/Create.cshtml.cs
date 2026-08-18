using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly LessonService _lessons;
    private readonly ModuleService _modules;
    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;

    public CreateModel(
        LessonService lessons,
        ModuleService modules,
        EnrollmentService enrollments,
        NotificationService notifications)
    {
        _lessons = lessons;
        _modules = modules;
        _enrollments = enrollments;
        _notifications = notifications;
    }

    public Module? Module { get; set; }

    [BindProperty]
    public int ModuleId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int moduleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var module = await _modules.GetByIdAsync(moduleId);
        if (module is null)
        {
            return NotFound();
        }

        if (module.Course is null || module.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Module = module;
        ModuleId = moduleId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!ModelState.IsValid)
        {
            Module = await _modules.GetByIdAsync(ModuleId);
            return Page();
        }

        var lesson = await _lessons.AddAsync(ModuleId, userId, Input.Title, Input.Content);
        if (lesson is null)
        {
            return Forbid();
        }

        var course = await _modules.GetByIdAsync(ModuleId);
        var courseId = course!.CourseId;
        if (courseId > 0)
        {
            // Notify every enrolled student that a new lesson was added.
            var (enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(courseId);
            await _notifications.CreateForManyAsync(
                enrollments.Select(e => e.StudentId),
                NotificationType.Lesson,
                $"New lesson in {course.Title}",
                $"{Input.Title} has been published.",
                $"/Courses/Details?id={courseId}");
        }

        return RedirectToPage("/Courses/Edit", new { id = courseId });
    }
}
