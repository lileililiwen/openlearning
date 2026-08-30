using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Authoring.Models;
using OpenLearning.Authoring.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;

namespace OpenLearning.Web.Pages.Courses.Revisions;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "教师工作台:/Dashboard/Teacher", "课程版本管理")]
public class IndexModel : PageModel
{
    private readonly RevisionLifecycleService _revisions;
    private readonly ApplicationDbContext _db;

    public IndexModel(RevisionLifecycleService revisions, ApplicationDbContext db)
    {
        _revisions = revisions;
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public CourseRevision? ActiveRevision { get; set; }

    public CourseRevision? DraftRevision { get; set; }

    public List<RevisionAudit> History { get; set; } = new();

    public List<ValidationError> ValidationErrors { get; set; } = new();

    public bool HasActiveRevision => ActiveRevision is not null;

    public bool HasDraft => DraftRevision is not null;

    public async Task<IActionResult> OnGetAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        var pointer = await _db.Set<CourseRevisionPointer>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.CourseId == CourseId);

        if (pointer is not null)
        {
            if (pointer.ActiveRevisionId is { } activeId)
            {
                ActiveRevision = await _db.Set<CourseRevision>().AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == activeId);
            }

            if (pointer.DraftRevisionId is { } draftId)
            {
                DraftRevision = await _db.Set<CourseRevision>().AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == draftId);
            }

            History = await _db.Set<RevisionAudit>().AsNoTracking()
                .Where(a => a.CourseId == CourseId)
                .OrderByDescending(a => a.At)
                .ToListAsync();

            var latest = await _db.Set<RevisionValidationResult>().AsNoTracking()
                .Where(v => v.CourseId == CourseId)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync();
            if (latest is not null)
            {
                ValidationErrors = JsonSerializer.Deserialize<List<ValidationError>>(latest.ErrorsJson) ?? new List<ValidationError>();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartEditingAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        await _revisions.EnsureDraftAsync(CourseId, course.InstructorId);
        TempData["Message"] = "已创建草稿，您可以在发布前继续编辑。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        try
        {
            await _revisions.GetPreviewAsync(CourseId, course.InstructorId);
        }
        catch (Exception ex) when (ex is UnauthorizedRevisionOperationException or InvalidOperationException)
        {
            TempData["Message"] = ex.Message;
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { CourseId });
        }

        TempData["Message"] = "已生成所有者预览（仅您可见，不会进入公开目录）。";
        TempData["MessageType"] = "info";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        var outcome = await _revisions.ValidateDraftAsync(CourseId, course.InstructorId);
        TempData["Message"] = outcome.IsValid
            ? "校验通过，可以发布。"
            : "校验发现以下问题，请修正后再发布。";
        TempData["MessageType"] = outcome.IsValid ? "success" : "warning";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostPublishAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        PublishResult result;
        try
        {
            result = await _revisions.PublishAsync(CourseId, course.InstructorId);
        }
        catch (RevisionConcurrencyException)
        {
            TempData["Message"] = "发布冲突：另一名教师同时发布了该课程，请刷新后重试。";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { CourseId });
        }

        if (!result.Succeeded)
        {
            TempData["Message"] = "草稿校验未通过，无法发布。请先点击“校验”查看错误。";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { CourseId });
        }

        TempData["Message"] = "已发布为新的生效版本，学习者现在可以看到该版本。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostUnpublishAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        await _revisions.UnpublishAsync(CourseId, course.InstructorId);
        TempData["Message"] = "已下架，学习者将不再看到该课程。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostRollbackAsync(int targetRevisionId)
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course;
        if (course is null)
        {
            return NotFound();
        }

        try
        {
            await _revisions.RollbackAsync(CourseId, course.InstructorId, targetRevisionId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Message"] = ex.Message;
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { CourseId });
        }

        TempData["Message"] = "已从所选版本创建新的草稿，历史记录保持不变。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { CourseId });
    }

    private async Task<IActionResult?> EnsureAccessAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CourseId);
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
}
