using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams;

[Authorize(Policy = Policies.RequireInstructor)]
public class ImportFromBankModel : PageModel
{
    private readonly QuestionBankService _bank;
    private readonly ExamService _exams;

    public ImportFromBankModel(QuestionBankService bank, ExamService exams)
    {
        _bank = bank;
        _exams = exams;
    }

    public List<Question> Items { get; set; } = new();

    public int ExamId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Text { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Topic { get; set; }

    public async Task<IActionResult> OnGetAsync(int examId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _exams.IsOwnerAsync(examId, userId))
        {
            return Forbid();
        }

        ExamId = examId;
        (Items, _) = await _bank.SearchAsync(Topic, Text, 1, 30);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int examId, int[] selected)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var imported = 0;
        foreach (var id in selected)
        {
            var (ok, _) = await _exams.ImportFromBankAsync(id, examId, userId);
            if (ok)
            {
                imported++;
            }
        }

        TempData["Message"] = $"已导入 {imported} 道题目。";
        return RedirectToPage("/Courses/Exams/Edit", new { id = examId });
    }
}
