using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OpenLearning.Assessments.Services;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Credits.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Scorm.Models;
using OpenLearning.Scorm.Services;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "我的课程:/MyCourses", "课程内容")]
public class ViewModel : PageModel
{
    private readonly LessonService _lessons;
    private readonly ModuleService _modules;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly IResumeService _resume;
    private readonly ScormService _scorm;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StudyToolService _studyTools;
    private readonly ChatService _chat;
    private readonly CreditService _credits;
    private readonly LessonNavigatorService _navigator;
    private readonly QuizService _quizzes;
    private readonly AssignmentService _assignments;

    public ViewModel(
        LessonService lessons,
        ModuleService modules,
        EnrollmentService enrollments,
        ProgressService progress,
        IResumeService resume,
        ScormService scorm,
        UserManager<ApplicationUser> userManager,
        StudyToolService studyTools,
        ChatService chat,
        CreditService credits,
        LessonNavigatorService navigator,
        QuizService quizzes,
        AssignmentService assignments)
    {
        _lessons = lessons;
        _modules = modules;
        _enrollments = enrollments;
        _progress = progress;
        _resume = resume;
        _scorm = scorm;
        _userManager = userManager;
        _studyTools = studyTools;
        _chat = chat;
        _credits = credits;
        _navigator = navigator;
        _quizzes = quizzes;
        _assignments = assignments;
    }

    public Lesson? Lesson { get; set; }

    public List<Lesson> ModuleLessons { get; set; } = new();

    public IReadOnlyList<ModuleOutline> Curriculum { get; set; } = Array.Empty<ModuleOutline>();

    public int? PrevLessonId { get; set; }

    public int? NextLessonId { get; set; }

    public string? AssessmentLinkLabel { get; set; }

    public string? AssessmentLinkUrl { get; set; }

    public ScormPackage? ScormPackage { get; set; }

    public bool CanTrackProgress { get; set; }

    public bool IsCompleted { get; set; }

    /// <summary>Total counted study time on this lesson, for the current Student.</summary>
    public int LessonDurationSeconds { get; set; }

    public LessonNote? Note { get; set; }

    public List<LessonDownload> Downloads { get; set; } = new();

    /// <summary>Existing danmu (bullet comments) for the lesson, oldest first.</summary>
    public List<DanmuItem> Danmu { get; set; } = new();

    /// <summary>True when the lesson is a video lesson.</summary>
    public bool IsVideoLesson => Lesson?.VideoUrl is not null;

    /// <summary>True for enrolled students on a video lesson (playback protections apply).</summary>
    public bool IsProtected { get; set; }

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

    private bool IsAjaxRequest =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsSuspendedAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.IsSuspended == true;
    }

    /// <summary>
    /// Reloads the enrolled-learner state (progress + notes) used by both the full page and the
    /// progressive-enhancement partial. Call after a mutation so the returned region reflects it.
    /// </summary>
    private async Task LoadEnrolledStateAsync(string userId, Lesson lesson)
    {
        CanTrackProgress = true;
        var completed = await _progress.GetCompletedLessonIdsAsync(userId, lesson.Module!.CourseId);
        IsCompleted = completed.Contains(lesson.Id);
        await _resume.RecordViewAsync(userId, lesson.Module.CourseId, lesson.Id);
        LessonDurationSeconds = await _progress.GetLessonDurationAsync(userId, lesson.Id);
        Note = await _studyTools.GetNoteAsync(userId, lesson.Id);
        NoteBody = Note?.Body ?? string.Empty;
        Downloads = await _studyTools.GetDownloadsAsync(lesson.Id);
        if (!string.IsNullOrWhiteSpace(lesson.VideoUrl))
        {
            IsProtected = true;
            Danmu = await _chat.GetLessonDanmuAsync(lesson.Id, 200);
        }
    }

    /// <summary>Returns the lesson-actions region for an AJAX request, or the full page otherwise.</summary>
    private async Task<IActionResult> LessonActionResultAsync(string userId, Lesson lesson, string? toastMessage, string? toastType)
    {
        if (!IsAjaxRequest)
        {
            if (toastMessage is not null)
            {
                TempData["Message"] = toastMessage;
                TempData["MessageType"] = toastType ?? "success";
            }

            return RedirectToPage(new { id = lesson.Id });
        }

        if (toastMessage is not null)
        {
            Response.Headers["X-Toast-Message"] = toastMessage;
            Response.Headers["X-Toast-Type"] = toastType ?? "success";
        }

        await LoadEnrolledStateAsync(userId, lesson);
        return new PartialViewResult
        {
            ViewName = "_LessonActions",
            ViewData = new ViewDataDictionary(ViewData) { Model = this }
        };
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

        if (course.Status != CourseStatus.Published && !isOwner && !isAdmin)
        {
            return Forbid();
        }

        var isEnrolled = userId is not null && await _enrollments.IsEnrolledAsync(userId, course.Id);
        var canPreview = lesson.IsPreview && course.IsPublished;
        if (course.IsPublished && !isOwner && !isAdmin && !isEnrolled && !canPreview)
        {
            return Forbid();
        }

        Lesson = lesson;
        ModuleLessons = await _modules.GetLessonsAsync(lesson.ModuleId);
        ScormPackage = await _scorm.GetForLessonAsync(id);

        var completedIds = userId is not null && isEnrolled
            ? await _progress.GetCompletedLessonIdsAsync(userId, course.Id)
            : new HashSet<int>();
        Curriculum = await _navigator.GetOrderedLessonsAsync(course.Id, completedIds);
        var nav = await _navigator.GetNavigatorAsync(course.Id, id);
        PrevLessonId = nav.PrevLessonId;
        NextLessonId = nav.NextLessonId;

        var quizzes = await _quizzes.GetForCourseAsync(course.Id);
        var firstQuiz = quizzes.FirstOrDefault();
        if (firstQuiz is not null)
        {
            AssessmentLinkLabel = $"Quiz: {firstQuiz.Title}";
            AssessmentLinkUrl = $"/Practice/Quiz?id={firstQuiz.Id}";
        }
        else
        {
            var courseAssignments = await _assignments.GetForCourseAsync(course.Id);
            var firstAssignment = courseAssignments.FirstOrDefault();
            if (firstAssignment is not null)
            {
                AssessmentLinkLabel = $"Assignment: {firstAssignment.Title}";
                AssessmentLinkUrl = $"/Courses/Assignments/Detail?id={firstAssignment.Id}";
            }
        }

        if (userId is not null && isEnrolled)
        {
            await LoadEnrolledStateAsync(userId, lesson);
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

        if (await _enrollments.IsAccessExpiredAsync(userId, lesson.Module.CourseId))
        {
            return await LessonActionResultAsync(userId, lesson, "Your access to this course has expired. Please renew to continue learning.", "danger");
        }

        var result = await _progress.MarkCompleteAsync(userId, lesson.Module.CourseId, id);
        if (!result.Ok)
        {
            return await LessonActionResultAsync(userId, lesson, result.Error, "danger");
        }

        if (await _progress.GetProgressPercentAsync(userId, lesson.Module.CourseId) == 100)
        {
            await _credits.ProcessCourseCompletionAsync(userId, lesson.Module.CourseId);
        }

        return await LessonActionResultAsync(userId, lesson, "Lesson marked as completed.", "success");
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

        if (await _enrollments.IsAccessExpiredAsync(userId, lesson.Module.CourseId))
        {
            return await LessonActionResultAsync(userId, lesson, "Your access to this course has expired. Please renew to continue learning.", "danger");
        }

        await _progress.UnmarkAsync(userId, lesson.Module.CourseId, id);
        return await LessonActionResultAsync(userId, lesson, "Lesson marked as not completed.", "success");
    }

    public async Task<IActionResult> OnPostCompleteAndNextAsync(int id)
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

        var courseId = lesson.Module.CourseId;
        if (await _enrollments.IsAccessExpiredAsync(userId, courseId))
        {
            TempData["Message"] = "Your access to this course has expired. Please renew to continue learning.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        var result = await _progress.MarkCompleteAsync(userId, courseId, id);
        if (!result.Ok)
        {
            TempData["Message"] = result.Error;
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        if (await _progress.GetProgressPercentAsync(userId, courseId) == 100)
        {
            await _credits.ProcessCourseCompletionAsync(userId, courseId);
        }

        var nav = await _navigator.GetNavigatorAsync(courseId, id);
        if (nav.NextLessonId is { } nextId)
        {
            return RedirectToPage(new { id = nextId });
        }

        return RedirectToPage("/Courses/Details", new { id = courseId });
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

        // Preserve the entered text on validation failure so the learner can correct and resubmit.
        if (!ModelState.IsValid)
        {
            return await LessonActionResultAsync(userId, lesson, "Please correct the note and try again.", "danger");
        }

        var (ok, error) = await _studyTools.UpsertNoteAsync(userId, id, NoteBody);
        return await LessonActionResultAsync(userId, lesson, ok ? "Note saved." : error, ok ? "success" : "danger");
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
