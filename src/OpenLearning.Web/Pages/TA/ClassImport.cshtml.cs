using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.StudentIO.Models;
using OpenLearning.StudentIO.Services;

namespace OpenLearning.Web.Pages.TA;

[Authorize(Policy = Policies.RequireTeachingAssistant)]
public class ClassImportModel : PageModel
{
    private readonly StudentImportService _import;
    private readonly ClassGroupService _classes;
    private readonly IClassAssignmentLookup _lookup;

    public ClassImportModel(StudentImportService import, ClassGroupService classes, IClassAssignmentLookup lookup)
    {
        _import = import;
        _classes = classes;
        _lookup = lookup;
    }

    public ClassGroup? Class { get; set; }

    public StudentImportOutcome? Outcome { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ClassId { get; set; }

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    [BindProperty]
    public StudentRowAction DefaultAction { get; set; } = StudentRowAction.CreateAndEnroll;

    public async Task<IActionResult> OnGetAsync(int classId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, classId))
        {
            return Forbid();
        }

        Class = await _classes.GetByIdAsync(classId);
        ClassId = classId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int classId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, classId))
        {
            return Forbid();
        }

        Class = await _classes.GetByIdAsync(classId);
        ClassId = classId;
        Outcome = await _import.ImportAsync(
            ExcelFile, userId, new StudentImportScope(IsTa: true, RequiredClassGroupId: classId), DefaultAction, forceAsync: false);
        if (Outcome.Kind == StudentImportOutcomeKind.Error)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        return Page();
    }
}
