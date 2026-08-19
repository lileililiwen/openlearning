using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Admin.QuestionBank;

[Authorize(Policy = Policies.RequireAdmin)]
public class CreateModel : PageModel
{
    private readonly QuestionBankService _bank;

    public CreateModel(QuestionBankService bank)
    {
        _bank = bank;
    }

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

        public List<string> Options { get; set; } = new() { "", "", "", "" };

        public int? CorrectIndex { get; set; }

        public List<int> CorrectIndexes { get; set; } = new();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (question, error) = await _bank.CreateAsync(
            Input.Text, Input.Points, Input.QuestionType, Input.BankTopic, BuildOptions());
        if (question is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create the question.");
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
