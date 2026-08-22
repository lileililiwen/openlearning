using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams.Integrity;

/// <summary>Reviewer (course owner) incident queue.</summary>
public class IncidentsModel : PageModel
{
    private readonly ExamIntegrityService _integrity;

    public IncidentsModel(ExamIntegrityService integrity)
    {
        _integrity = integrity;
    }

    public List<OpenLearning.Exams.Models.IntegrityIncident> Items { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        Items = await _integrity.ListIncidentsForReviewerAsync(userId);
        return Page();
    }
}
