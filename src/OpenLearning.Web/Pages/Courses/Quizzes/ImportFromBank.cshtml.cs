using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class ImportFromBankModel : PageModel
{
    private readonly QuestionBankService _bank;
    private readonly QuizService _quizzes;

    public ImportFromBankModel(QuestionBankService bank, QuizService quizzes)
    {
        _bank = bank;
        _quizzes = quizzes;
    }

    public List<Question> Items { get; set; } = new();

    public int QuizId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Text { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Topic { get; set; }

    public async Task<IActionResult> OnGetAsync(int quizId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsOwnerAsync(quizId, userId))
        {
            return Forbid();
        }

        QuizId = quizId;
        (Items, _) = await _bank.SearchAsync(Topic, Text, 1, 30);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int quizId, int[] selected)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var imported = 0;
        foreach (var id in selected)
        {
            var (ok, _) = await _bank.ImportIntoQuizAsync(id, quizId, userId);
            if (ok)
            {
                imported++;
            }
        }

        TempData["Message"] = $"已导入 {imported} 道题目。";
        return RedirectToPage("/Courses/Quizzes/Edit", new { id = quizId });
    }
}
