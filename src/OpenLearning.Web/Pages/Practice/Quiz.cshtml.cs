using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Practice;

[Authorize(Policy = Policies.RequireStudent)]
public class QuizModel : PageModel
{
    private readonly IncorrectAnswerService _incorrect;

    public QuizModel(IncorrectAnswerService incorrect)
    {
        _incorrect = incorrect;
    }

    public List<Question> Questions { get; set; } = new();

    [BindProperty]
    public Dictionary<int, int> Answers { get; set; } = new();

    [BindProperty]
    public Dictionary<int, string[]> Multiple { get; set; } = new();

    [BindProperty]
    public Dictionary<int, string> TextAnswers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Questions = await _incorrect.BuildPracticeQuestionsAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var questions = await _incorrect.BuildPracticeQuestionsAsync(userId);
        var correctCount = 0;
        foreach (var question in questions)
        {
            string? selectedIds = null;
            var multiple = Multiple.GetValueOrDefault(question.Id);
            if (multiple is not null && multiple.Length > 0)
            {
                selectedIds = string.Join(",", multiple
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .OrderBy(x => x));
            }

            var scored = QuestionScoring.Score(
                question,
                Answers.GetValueOrDefault(question.Id),
                selectedIds,
                TextAnswers.GetValueOrDefault(question.Id),
                null);
            if (scored.IsCorrect)
            {
                correctCount++;
                await _incorrect.ResolveAsync(userId, question.Id);
            }
        }

        TempData["PracticeResult"] = questions.Count == 0
            ? "没有需要练习的题目。"
            : $"本次练习答对 {correctCount}/{questions.Count} 道。答对的题目已从错题本移除。";
        return RedirectToPage("/Practice/Index");
    }
}
