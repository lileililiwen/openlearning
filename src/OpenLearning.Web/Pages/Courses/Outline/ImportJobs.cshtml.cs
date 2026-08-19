using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.CourseOutlineIO.Models;

namespace OpenLearning.Web.Pages.Courses.Outline;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class ImportJobsModel : PageModel
{
    private readonly CourseService _courses;
    private readonly DbContext _db;

    public ImportJobsModel(CourseService courses, DbContext db)
    {
        _courses = courses;
        _db = db;
    }

    public string CourseTitle { get; set; } = string.Empty;

    public List<OutlineImportJob> Jobs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int CourseId { get; set; }

    public async Task<IActionResult> OnGetAsync()
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

        CourseTitle = course.Title;
        Jobs = await _db.Set<OutlineImportJob>().AsNoTracking()
            .Where(j => j.CourseId == CourseId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(20)
            .ToListAsync();
        return Page();
    }

    public static string ModeLabel(OutlineImportMode mode)
    {
        return mode == OutlineImportMode.Replace ? "替换" : "追加";
    }

    public static string StatusLabel(OutlineImportJobStatus status)
    {
        return status switch
        {
            OutlineImportJobStatus.Pending => "等待中",
            OutlineImportJobStatus.Running => "导入中",
            OutlineImportJobStatus.Success => "已完成",
            _ => "失败",
        };
    }
}
