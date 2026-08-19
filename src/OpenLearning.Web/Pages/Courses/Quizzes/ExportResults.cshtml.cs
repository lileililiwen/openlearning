using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.GradeExport.Models;
using OpenLearning.GradeExport.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class ExportResultsModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly QuizService _quizzes;
    private readonly CourseService _courses;
    private readonly GradeExportService _export;
    private readonly SystemConfigService _config;

    public ExportResultsModel(
        QuizService quizzes,
        CourseService courses,
        GradeExportService export,
        SystemConfigService config)
    {
        _quizzes = quizzes;
        _courses = courses;
        _export = export;
        _config = config;
    }

    public List<Quiz> Quizzes { get; set; } = new();

    public string CourseTitle { get; set; } = string.Empty;

    public int? JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CourseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? QuizId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    private async Task<IActionResult?> LoadContextAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(CourseId);
        if (course is null)
        {
            return NotFound();
        }

        if (course.InstructorId != userId && !User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        if (QuizId is int quizId)
        {
            var quiz = await _quizzes.GetByIdAsync(quizId);
            if (quiz is null || quiz.CourseId != CourseId)
            {
                return NotFound();
            }
        }

        CourseTitle = course.Title;
        Quizzes = await _quizzes.GetForCourseAsync(CourseId);
        return null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadContextAsync() ?? Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var access = await LoadContextAsync();
        if (access is not null)
        {
            return access;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var filters = new GradeExportFilters(
            CourseId: CourseId,
            AssignmentId: null,
            QuizId: QuizId,
            ExamId: null,
            ClassGroupId: null,
            From: ToUtcStart(From),
            To: ToUtcExclusive(To),
            GradedOnly: null,
            IsTaScope: false,
            IsAdmin: User.IsInRole(Roles.Admin));

        var count = await _export.CountAsync(GradeExportKind.QuizAttempts, filters, userId);
        var syncMax = await _config.GetIntAsync("grade.export.syncMaxRows", 1000);
        if (count > syncMax)
        {
            var (jobId, error) = await _export.SubmitAsync(GradeExportKind.QuizAttempts, filters, userId);
            if (error is not null)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            else
            {
                JobId = jobId;
            }

            return Page();
        }

        var (bytes, errorMessage, _) = await _export.ExportSyncAsync(GradeExportKind.QuizAttempts, filters, userId);
        if (errorMessage is not null || bytes is null)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "导出失败。");
            return Page();
        }

        var fileName = QuizId is int qid ? $"quiz-attempts-{qid}.xlsx" : $"course-quiz-attempts-{CourseId}.xlsx";
        return File(bytes, _contentTypeXlsx, fileName);
    }

    private static DateTime? ToUtcStart(DateTime? value)
    {
        return value is DateTime v ? DateTime.SpecifyKind(v.Date, DateTimeKind.Utc) : null;
    }

    private static DateTime? ToUtcExclusive(DateTime? value)
    {
        return value is DateTime v ? DateTime.SpecifyKind(v.Date.AddDays(1), DateTimeKind.Utc) : null;
    }
}
