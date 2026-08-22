using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams.Integrity;

/// <summary>Instructor policy management for an exam (or the global default).</summary>
public class PolicyModel : PageModel
{
    private readonly ExamService _exams;
    private readonly ExamIntegrityService _integrity;

    public PolicyModel(ExamService exams, ExamIntegrityService integrity)
    {
        _exams = exams;
        _integrity = integrity;
    }

    [BindProperty(SupportsGet = true)]
    public int? ExamId { get; set; }

    public string? ExamTitle { get; set; }

    public IntegrityPolicy? Effective { get; set; }

    public IntegrityPolicy? GlobalDefault { get; set; }

    [BindProperty]
    public int RiskThreshold { get; set; } = 100;

    [BindProperty]
    public int HeartbeatGapWeight { get; set; } = 25;

    [BindProperty]
    public int VisibilityHiddenWeight { get; set; } = 20;

    [BindProperty]
    public int TabSwitchWeight { get; set; } = 15;

    [BindProperty]
    public int CopyAttemptWeight { get; set; } = 15;

    [BindProperty]
    public int PasteAttemptWeight { get; set; } = 10;

    [BindProperty]
    public int ConnectivityLossWeight { get; set; } = 5;

    [BindProperty]
    public int RetentionDays { get; set; } = 90;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        if (ExamId.HasValue)
        {
            if (!await _exams.IsOwnerAsync(ExamId.Value, userId))
            {
                return Forbid();
            }

            var exam = await _exams.GetByIdAsync(ExamId.Value);
            ExamTitle = exam?.Title;
            Effective = await _integrity.GetEffectivePolicyAsync(ExamId.Value);
        }
        else
        {
            GlobalDefault = await _integrity.GetEffectivePolicyAsync(0);
            Effective = GlobalDefault;
        }

        if (Effective is not null)
        {
            RiskThreshold = Effective.RiskThreshold;
            HeartbeatGapWeight = Effective.HeartbeatGapWeight;
            VisibilityHiddenWeight = Effective.VisibilityHiddenWeight;
            TabSwitchWeight = Effective.TabSwitchWeight;
            CopyAttemptWeight = Effective.CopyAttemptWeight;
            PasteAttemptWeight = Effective.PasteAttemptWeight;
            ConnectivityLossWeight = Effective.ConnectivityLossWeight;
            RetentionDays = Effective.RetentionDays;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (ExamId.HasValue && !await _exams.IsOwnerAsync(ExamId.Value, userId))
        {
            return Forbid();
        }

        await _integrity.CreatePolicyAsync(
            ExamId, RiskThreshold, HeartbeatGapWeight, VisibilityHiddenWeight, TabSwitchWeight,
            CopyAttemptWeight, PasteAttemptWeight, ConnectivityLossWeight, RetentionDays, userId);

        if (ExamId.HasValue)
        {
            return RedirectToPage("/Courses/Exams/Integrity/Policy", new { examId = ExamId });
        }

        return RedirectToPage("/Courses/Exams/Integrity/Policy");
    }
}
