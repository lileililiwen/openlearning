using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Courses.Quizzes.Questions;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly QuestionService _questions;
    private readonly QuizService _quizzes;

    public CreateModel(QuestionService questions, QuizService quizzes)
    {
        _questions = questions;
        _quizzes = quizzes;
    }

    [BindProperty]
    public int QuizId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100.")]
        public int Points { get; set; } = 1;

        public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;

        public List<string> Options { get; set; } = new() { "", "", "", "" };

        public int? CorrectIndex { get; set; }

        public List<int> CorrectIndexes { get; set; } = new();
    }

    public async Task<IActionResult> OnGetAsync(int quizId)
    {
        QuizId = quizId;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsOwnerAsync(quizId, userId))
        {
            return Forbid();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse && !Input.CorrectIndex.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select which answer option is correct.");
            return Page();
        }

        if (Input.QuestionType == QuestionType.MultipleChoice && Input.CorrectIndexes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one correct answer.");
            return Page();
        }

        var options = BuildOptions();
        var (_, error) = await _questions.AddAsync(
            QuizId, userId, Input.Text, Input.Points, Input.QuestionType, options);

        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        return RedirectToPage("/Courses/Quizzes/Edit", new { id = QuizId });
    }

    private List<AnswerOptionInput> BuildOptions()
    {
        var options = new List<AnswerOptionInput>();
        for (var i = 0; i < Input.Options.Count; i++)
        {
            var text = Input.Options[i].Trim();
            if (text.Length == 0)
            {
                continue;
            }

            var isCorrect = Input.QuestionType switch
            {
                QuestionType.SingleChoice or QuestionType.TrueFalse => i == Input.CorrectIndex,
                QuestionType.MultipleChoice => Input.CorrectIndexes.Contains(i),
                QuestionType.FillBlank => true, // every provided answer is acceptable
                _ => false,
            };
            options.Add(new AnswerOptionInput(text, isCorrect));
        }

        return options;
    }
}
