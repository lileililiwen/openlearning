using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;

namespace OpenLearning.Web.Pages.Courses.Classes;

[Authorize(Policy = Policies.RequireInstructor)]
public class RosterModel : PageModel
{
    private readonly ClassGroupService _classes;
    private readonly ClassRosterService _roster;

    public RosterModel(ClassGroupService classes, ClassRosterService roster)
    {
        _classes = classes;
        _roster = roster;
    }

    public ClassGroup? ClassGroup { get; set; }

    public List<ClassRosterRow> Rows { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var classGroup = await _classes.GetByIdAsync(id);
        if (classGroup is null)
        {
            return NotFound();
        }

        if (classGroup.Course is null || classGroup.Course.InstructorId != userId)
        {
            return Forbid();
        }

        ClassGroup = classGroup;
        Rows = await _roster.GetRosterAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnGetExportCsvAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var classGroup = await _classes.GetByIdAsync(id);
        if (classGroup is null || classGroup.Course is null || classGroup.Course.InstructorId != userId)
        {
            return Forbid();
        }

        var rows = await _roster.GetRosterAsync(id);
        var csv = new StringBuilder();
        csv.AppendLine("StudentId,Name,Email,EnrolledAt");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(',',
                CsvEscape(row.StudentId),
                CsvEscape(row.StudentName),
                CsvEscape(row.StudentEmail),
                row.EnrolledAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"class-{id}-roster.csv");
    }

    private static string CsvEscape(string value)
    {
        var v = value ?? string.Empty;
        return v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
    }
}
