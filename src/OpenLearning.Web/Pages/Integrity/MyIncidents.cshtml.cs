using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Integrity;

/// <summary>Learner view of their own integrity incidents, with appeal submission.</summary>
public class MyIncidentsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ExamIntegrityService _integrity;

    public MyIncidentsModel(ApplicationDbContext db, ExamIntegrityService integrity)
    {
        _db = db;
        _integrity = integrity;
    }

    public List<IntegrityIncident> Items { get; set; } = new();

    [BindProperty]
    public int IncidentId { get; set; }

    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        Items = await _db.IntegrityIncidents
            .Where(i => i.StudentId == userId)
            .OrderByDescending(i => i.DetectedAt)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAppealAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _integrity.SubmitAppealAsync(IncidentId, userId, Reason);
        if (result.Error is not null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await OnGetAsync();
            return Page();
        }

        return RedirectToPage();
    }
}
