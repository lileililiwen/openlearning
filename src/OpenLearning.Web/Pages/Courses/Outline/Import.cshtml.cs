using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.CourseOutlineIO.Models;
using OpenLearning.CourseOutlineIO.Services;

namespace OpenLearning.Web.Pages.Courses.Outline;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class ImportModel : PageModel
{
    private readonly CourseService _courses;
    private readonly OutlineImportService _import;

    public ImportModel(CourseService courses, OutlineImportService import)
    {
        _courses = courses;
        _import = import;
    }

    public Course? Course { get; set; }

    public OutlineImportOutcome? Outcome { get; set; }

    public OutlineReplacePreview? ReplacePreview { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CourseId { get; set; }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public string Mode { get; set; } = "append";

    [BindProperty]
    public bool ConfirmReplace { get; set; }

    [BindProperty]
    public bool ForceAsync { get; set; }

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

        Course = course;
        return null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var access = await LoadContextAsync();
        if (access is not null)
        {
            return access;
        }

        ReplacePreview = await _import.PreflightReplaceAsync(CourseId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var access = await LoadContextAsync();
        if (access is not null)
        {
            return access;
        }

        var mode = Mode == "replace" ? OutlineImportMode.Replace : OutlineImportMode.Append;
        if (mode == OutlineImportMode.Replace && !ConfirmReplace)
        {
            ModelState.AddModelError(string.Empty, "请勾选确认，以执行“替换”模式导入。");
            ReplacePreview = await _import.PreflightReplaceAsync(CourseId);
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var outcome = await _import.ImportAsync(
            UploadFile, userId, CourseId, mode, User.IsInRole(Roles.Admin), ForceAsync);
        Outcome = outcome;
        ReplacePreview = await _import.PreflightReplaceAsync(CourseId);
        return Page();
    }
}
