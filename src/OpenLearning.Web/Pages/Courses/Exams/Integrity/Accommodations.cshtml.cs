using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams.Integrity;

/// <summary>Instructor accommodation management. Only operational adjustments are shown.</summary>
public class AccommodationsModel : PageModel
{
    private readonly ExamService _exams;
    private readonly ExamIntegrityService _integrity;
    private readonly ApplicationDbContext _db;

    public AccommodationsModel(ExamService exams, ExamIntegrityService integrity, ApplicationDbContext db)
    {
        _exams = exams;
        _integrity = integrity;
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int ExamId { get; set; }

    public string? ExamTitle { get; set; }

    public List<LearnerAccommodation> Items { get; set; } = new();

    [BindProperty]
    public string StudentId { get; set; } = string.Empty;

    [BindProperty]
    public int ExtraMinutes { get; set; }

    [BindProperty]
    public int AllowedBreaks { get; set; }

    [BindProperty]
    public int RelaxedVisibilityThreshold { get; set; }

    [BindProperty]
    public int RelaxedCopyPasteThreshold { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _exams.IsOwnerAsync(ExamId, userId))
        {
            return Forbid();
        }

        var exam = await _exams.GetByIdAsync(ExamId);
        ExamTitle = exam?.Title;
        Items = await _db.LearnerAccommodations
            .Where(a => a.ExamId == ExamId)
            .OrderBy(a => a.StudentId)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGrantAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _exams.IsOwnerAsync(ExamId, userId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(StudentId))
        {
            ModelState.AddModelError(string.Empty, "Student id is required.");
            return await OnGetAsync();
        }

        await _integrity.GrantAccommodationAsync(
            ExamId, StudentId.Trim(), ExtraMinutes, AllowedBreaks,
            RelaxedVisibilityThreshold, RelaxedCopyPasteThreshold, userId);
        return RedirectToPage(new { examId = ExamId });
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var accommodation = await _db.LearnerAccommodations.FindAsync(id);
        if (accommodation is null || !await _exams.IsOwnerAsync(accommodation.ExamId, userId))
        {
            return Forbid();
        }

        await _integrity.RevokeAccommodationAsync(id);
        return RedirectToPage(new { examId = ExamId });
    }
}
