using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.GradeExport.Models;
using OpenLearning.GradeExport.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.TA;

[Authorize(Policy = Policies.RequireTeachingAssistant)]
public class ClassExportModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IClassAssignmentLookup _lookup;
    private readonly ClassGroupService _classes;
    private readonly GradeExportService _export;
    private readonly SystemConfigService _config;

    public ClassExportModel(
        IClassAssignmentLookup lookup,
        ClassGroupService classes,
        GradeExportService export,
        SystemConfigService config)
    {
        _lookup = lookup;
        _classes = classes;
        _export = export;
        _config = config;
    }

    public ClassGroup? ClassGroup { get; set; }

    public int? JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ClassId { get; set; }

    private async Task<IActionResult?> LoadContextAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, ClassId))
        {
            return Forbid();
        }

        ClassGroup = await _classes.GetByIdAsync(ClassId);
        if (ClassGroup is null)
        {
            return NotFound();
        }

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
            CourseId: null,
            AssignmentId: null,
            QuizId: null,
            ExamId: null,
            ClassGroupId: ClassId,
            From: null,
            To: null,
            GradedOnly: null,
            IsTaScope: true,
            IsAdmin: false);

        var count = await _export.CountAsync(GradeExportKind.CourseRoster, filters, userId);
        var syncMax = await _config.GetIntAsync("grade.export.syncMaxRows", 1000);
        if (count > syncMax)
        {
            var (jobId, error) = await _export.SubmitAsync(GradeExportKind.CourseRoster, filters, userId);
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

        var (bytes, errorMessage, _) = await _export.ExportSyncAsync(GradeExportKind.CourseRoster, filters, userId);
        if (errorMessage is not null || bytes is null)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "导出失败。");
            return Page();
        }

        return File(bytes, _contentTypeXlsx, $"class-roster-{ClassId}.xlsx");
    }
}
