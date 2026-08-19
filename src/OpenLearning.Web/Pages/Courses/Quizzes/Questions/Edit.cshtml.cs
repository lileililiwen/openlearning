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
public class EditModel : PageModel
{
    private readonly QuestionService _questions;

    public EditModel(QuestionService questions)
    {
        _questions = questions;
    }

    public Question? Question { get; set; }

    [BindProperty]
    public int Id { get; set; }

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

        public List<string> Options { get; set; } = new();

        public int? CorrectIndex { get; set; }

        public List<int> CorrectIndexes { get; set; } = new();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var question = await _questions.GetByIdAsync(id);
        if (question is null)
        {
            return NotFound();
        }

        if (question.Quiz?.Course is null || question.Quiz.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Question = question;
        Id = id;
        Input.Text = question.Text;
        Input.Points = question.Points;
        Input.QuestionType = question.QuestionType;

        var options = question.AnswerOptions.OrderBy(o => o.OrderIndex).ToList();
        Input.Options = options.Select(o => o.Text).ToList();
        Input.CorrectIndex = options.FindIndex(o => o.IsCorrect);
        Input.CorrectIndex = Input.CorrectIndex >= 0 ? Input.CorrectIndex : null;
        Input.CorrectIndexes = options.Where(o => o.IsCorrect).Select(o => o.OrderIndex - 1).ToList();
        while (Input.Options.Count < 4)
        {
            Input.Options.Add(string.Empty);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!ModelState.IsValid)
        {
            Question = await _questions.GetByIdAsync(Id);
            return Page();
        }

        if (Input.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse && !Input.CorrectIndex.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select which answer option is correct.");
            Question = await _questions.GetByIdAsync(Id);
            return Page();
        }

        if (Input.QuestionType == QuestionType.MultipleChoice && Input.CorrectIndexes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one correct answer.");
            Question = await _questions.GetByIdAsync(Id);
            return Page();
        }

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

        var (ok, error) = await _questions.UpdateAsync(
            Id, userId, Input.Text, Input.Points, Input.QuestionType, options);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to save question.");
            Question = await _questions.GetByIdAsync(Id);
            return Page();
        }

        var quizId = (await _questions.GetByIdAsync(Id))!.QuizId;
        return RedirectToPage("/Courses/Quizzes/Edit", new { id = quizId });
    }
}
