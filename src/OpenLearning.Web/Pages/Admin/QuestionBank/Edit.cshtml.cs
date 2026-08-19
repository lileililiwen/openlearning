using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Admin.QuestionBank;

[Authorize(Policy = Policies.RequireAdmin)]
public class EditModel : PageModel
{
    private readonly QuestionBankService _bank;

    public EditModel(QuestionBankService bank)
    {
        _bank = bank;
    }

    public Question? Question { get; set; }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [StringLength(200)]
        public string? BankTopic { get; set; }

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
        var question = await _bank.GetByIdAsync(id);
        if (question is null)
        {
            return NotFound();
        }

        Question = question;
        Id = id;
        Input.BankTopic = question.BankTopic;
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
        if (!ModelState.IsValid)
        {
            Question = await _bank.GetByIdAsync(Id);
            return Page();
        }

        var (ok, error) = await _bank.UpdateAsync(
            Id, Input.Text, Input.Points, Input.QuestionType, Input.BankTopic, BuildOptions());
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to save the question.");
            Question = await _bank.GetByIdAsync(Id);
            return Page();
        }

        return RedirectToPage("/Admin/QuestionBank/Index");
    }

    private List<AnswerOptionInput> BuildOptions()
    {
        var options = new List<AnswerOptionInput>();
        for (var i = 0; i < Input.Options.Count; i++)
        {
            var text = (Input.Options[i] ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                continue;
            }

            var isCorrect = Input.QuestionType switch
            {
                QuestionType.SingleChoice or QuestionType.TrueFalse => i == Input.CorrectIndex,
                QuestionType.MultipleChoice => Input.CorrectIndexes.Contains(i),
                QuestionType.FillBlank => true,
                _ => false,
            };
            options.Add(new AnswerOptionInput(text, isCorrect));
        }

        return options;
    }
}
