using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Scorm.Models;
using OpenLearning.Scorm.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

public class ViewModel : PageModel
{
    private readonly LessonService _lessons;
    private readonly ModuleService _modules;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly ScormService _scorm;

    public ViewModel(LessonService lessons, ModuleService modules, EnrollmentService enrollments, ProgressService progress, ScormService scorm)
    {
        _lessons = lessons;
        _modules = modules;
        _enrollments = enrollments;
        _progress = progress;
        _scorm = scorm;
    }

    public Lesson? Lesson { get; set; }

    public List<Lesson> ModuleLessons { get; set; } = new();

    public ScormPackage? ScormPackage { get; set; }

    public bool CanTrackProgress { get; set; }

    public bool IsCompleted { get; set; }

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

        Lesson = lesson;
        ModuleLessons = await _modules.GetLessonsAsync(lesson.ModuleId);
        ScormPackage = await _scorm.GetForLessonAsync(id);
        if (userId is not null && isEnrolled)
        {
            CanTrackProgress = true;
            var completed = await _progress.GetCompletedLessonIdsAsync(userId, course.Id);
            IsCompleted = completed.Contains(id);
            await _progress.RecordAccessAsync(userId, course.Id, id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson?.Module?.Course is null)
        {
            return NotFound();
        }

        await _progress.MarkCompleteAsync(userId, lesson.Module.CourseId, id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUncompleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson?.Module?.Course is null)
        {
            return NotFound();
        }

        await _progress.UnmarkAsync(userId, lesson.Module.CourseId, id);
        return RedirectToPage(new { id });
    }
}
