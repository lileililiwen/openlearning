using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.GradeExport.Models;
using OpenLearning.GradeExport.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments;

[Authorize(Policy = Policies.RequireInstructor)]
public class ExportModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;
    private readonly GradeExportService _export;
    private readonly SystemConfigService _config;

    public ExportModel(
        AssignmentService assignments,
        CourseService courses,
        GradeExportService export,
        SystemConfigService config)
    {
        _assignments = assignments;
        _courses = courses;
        _export = export;
        _config = config;
    }

    public Assignment? Assignment { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public int? JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CourseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int AssignmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

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

        var assignment = await _assignments.GetByIdAsync(AssignmentId);
        if (assignment is null || assignment.CourseId != CourseId)
        {
            return NotFound();
        }

        Assignment = assignment;
        CourseTitle = course.Title;
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
            AssignmentId: AssignmentId,
            QuizId: null,
            ExamId: null,
            ClassGroupId: null,
            From: ToUtcStart(From),
            To: ToUtcExclusive(To),
            GradedOnly: ParseStatus(Status),
            IsTaScope: false,
            IsAdmin: User.IsInRole(Roles.Admin));
        return await RunExportAsync(GradeExportKind.Submissions, filters, userId);
    }

    private async Task<IActionResult> RunExportAsync(GradeExportKind kind, GradeExportFilters filters, string userId)
    {
        var count = await _export.CountAsync(kind, filters, userId);
        var syncMax = await _config.GetIntAsync("grade.export.syncMaxRows", 1000);
        if (count > syncMax)
        {
            var (jobId, error) = await _export.SubmitAsync(kind, filters, userId);
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

        var (bytes, errorMessage, _) = await _export.ExportSyncAsync(kind, filters, userId);
        if (errorMessage is not null || bytes is null)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "导出失败。");
            return Page();
        }

        return File(bytes, _contentTypeXlsx, $"submissions-{AssignmentId}.xlsx");
    }

    private static DateTime? ToUtcStart(DateTime? value)
    {
        return value is DateTime v ? DateTime.SpecifyKind(v.Date, DateTimeKind.Utc) : null;
    }

    private static DateTime? ToUtcExclusive(DateTime? value)
    {
        return value is DateTime v ? DateTime.SpecifyKind(v.Date.AddDays(1), DateTimeKind.Utc) : null;
    }

    private static bool? ParseStatus(string? status)
    {
        return status switch
        {
            "graded" => true,
            "ungraded" => false,
            _ => null,
        };
    }
}
