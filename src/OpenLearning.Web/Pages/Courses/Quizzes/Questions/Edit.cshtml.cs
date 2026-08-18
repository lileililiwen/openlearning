using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;

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

        public List<string> Options { get; set; } = new();

        public int? CorrectIndex { get; set; }
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

        var options = question.AnswerOptions.OrderBy(o => o.OrderIndex).ToList();
        Input.Options = options.Select(o => o.Text).ToList();
        var correctIndex = options.FindIndex(o => o.IsCorrect);
        Input.CorrectIndex = correctIndex >= 0 ? correctIndex : null;
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

        if (!Input.CorrectIndex.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select which answer option is correct.");
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

            options.Add(new AnswerOptionInput(text, i == Input.CorrectIndex));
        }

        var (ok, error) = await _questions.UpdateAsync(
            Id, userId, Input.Text, Input.Points, options);

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
