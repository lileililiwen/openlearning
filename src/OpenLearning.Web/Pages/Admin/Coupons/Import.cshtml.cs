using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CouponIO.Services;

namespace OpenLearning.Web.Pages.Admin.Coupons;

[Authorize(Policy = Policies.RequireAdmin)]
public class ImportModel : PageModel
{
    private readonly CouponImportService _import;

    public ImportModel(CouponImportService import)
    {
        _import = import;
    }

    public CouponImportOutcome? Outcome { get; set; }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public bool ForceAsync { get; set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var outcome = await _import.ImportAsync(UploadFile, adminId, ForceAsync);
        Outcome = outcome;
        if (outcome.Kind == CouponImportOutcomeKind.RateLimited)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        }

        return Page();
    }
}
