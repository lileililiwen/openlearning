using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Scorm.Models;
using OpenLearning.Scorm.Services;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

public class ViewModel : PageModel
{
    private readonly LessonService _lessons;
    private readonly ModuleService _modules;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly ScormService _scorm;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StudyToolService _studyTools;

    public ViewModel(
        LessonService lessons,
        ModuleService modules,
        EnrollmentService enrollments,
        ProgressService progress,
        ScormService scorm,
        UserManager<ApplicationUser> userManager,
        StudyToolService studyTools)
    {
        _lessons = lessons;
        _modules = modules;
        _enrollments = enrollments;
        _progress = progress;
        _scorm = scorm;
        _userManager = userManager;
        _studyTools = studyTools;
    }

    public Lesson? Lesson { get; set; }

    public List<Lesson> ModuleLessons { get; set; } = new();

    public ScormPackage? ScormPackage { get; set; }

    public bool CanTrackProgress { get; set; }

    public bool IsCompleted { get; set; }

    /// <summary>Total counted study time on this lesson, for the current Student.</summary>
    public int LessonDurationSeconds { get; set; }

    public LessonNote? Note { get; set; }

    public List<LessonDownload> Downloads { get; set; } = new();

    [BindProperty]
    public string NoteBody { get; set; } = string.Empty;

    public static string FormatDuration(int seconds)
    {
        var totalMinutes = (int)Math.Ceiling(seconds / 60.0);
        if (totalMinutes < 60)
        {
            return $"{totalMinutes} min";
        }

        return $"{(totalMinutes / 60)} h {totalMinutes % 60} min";
    }

    private async Task<bool> IsSuspendedAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.IsSuspended == true;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson?.Module?.Course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null && await IsSuspendedAsync())
        {
            return Forbid();
        }

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
            LessonDurationSeconds = await _progress.GetLessonDurationAsync(userId, id);
            Note = await _studyTools.GetNoteAsync(userId, id);
            NoteBody = Note?.Body ?? string.Empty;
            Downloads = await _studyTools.GetDownloadsAsync(id);
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

        if (await IsSuspendedAsync())
        {
            return Forbid();
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

        if (await IsSuspendedAsync())
        {
            return Forbid();
        }

        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson?.Module?.Course is null)
        {
            return NotFound();
        }

        await _progress.UnmarkAsync(userId, lesson.Module.CourseId, id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSaveNoteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (await IsSuspendedAsync())
        {
            return Forbid();
        }

        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson?.Module?.Course is null)
        {
            return NotFound();
        }

        if (!await _enrollments.IsEnrolledAsync(userId, lesson.Module.CourseId))
        {
            return Forbid();
        }

        var (ok, error) = await _studyTools.UpsertNoteAsync(userId, id, NoteBody);
        TempData["Message"] = ok ? "Note saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetExportNoteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var note = await _studyTools.GetNoteAsync(userId, id);
        if (note is null)
        {
            return NotFound();
        }

        var lesson = await _lessons.GetByIdAsync(id);
        var title = lesson?.Title ?? "Lesson note";
        var fileName = $"{string.Concat(title.Where(c => char.IsLetterOrDigit(c) || c == '-'))}.md";
        var bytes = Encoding.UTF8.GetBytes(StudyToolService.ToMarkdown(title, note.Body));
        return File(bytes, "text/markdown", fileName);
    }
}
