using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.StudentIO.Models;
using OpenLearning.StudentIO.Services;

namespace OpenLearning.Web.Pages.Admin.Students;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public class ImportModel : PageModel
{
    private readonly StudentImportService _import;

    public ImportModel(StudentImportService import)
    {
        _import = import;
    }

    public StudentImportOutcome? Outcome { get; set; }

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    [BindProperty]
    public StudentRowAction DefaultAction { get; set; } = StudentRowAction.Create;

    [BindProperty]
    public bool ForceAsync { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Outcome = await _import.ImportAsync(
            ExcelFile, userId, new StudentImportScope(IsTa: false), DefaultAction, ForceAsync);
        if (Outcome.Kind == StudentImportOutcomeKind.Error)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        return Page();
    }
}
