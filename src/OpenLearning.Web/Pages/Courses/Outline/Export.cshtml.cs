using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseOutlineIO.Services;

namespace OpenLearning.Web.Pages.Courses.Outline;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class ExportModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly OutlineExportService _export;

    public ExportModel(OutlineExportService export)
    {
        _export = export;
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (bytes, error) = await _export.ExportAsync(courseId, userId, User.IsInRole(Roles.Admin));
        if (error is not null || bytes is null)
        {
            TempData["Message"] = error ?? "导出失败。";
            TempData["MessageType"] = "danger";
            return RedirectToPage("/Courses/Edit", new { id = courseId });
        }

        return File(bytes, _contentTypeXlsx, $"outline-{courseId}.xlsx");
    }
}
