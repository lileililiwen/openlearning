using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Surveys.Models;
using OpenLearning.Surveys.Services;

namespace OpenLearning.Web.Pages.Surveys;

[Authorize]
public class TakeModel : PageModel
{
    private readonly SurveyService _surveys;

    public TakeModel(SurveyService surveys)
    {
        _surveys = surveys;
    }

    public Survey? Survey { get; set; }

    public bool AlreadyResponded { get; set; }

    public bool WindowClosed { get; set; }

    public int? CourseId { get; set; }

    public sealed class AnswerRow
    {
        public int QuestionId { get; set; }

        public List<int> OptionIds { get; set; } = new();

        public int? RatingValue { get; set; }

        public string? TextValue { get; set; }
    }

    [BindProperty]
    public List<AnswerRow> Answers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, int? courseId)
    {
        var load = await LoadAsync(id, courseId);
        return load ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, int? courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var answers = Answers.ToDictionary(
            a => a.QuestionId,
            a => new SurveyService.AnswerInput(a.QuestionId, a.OptionIds, a.RatingValue, a.TextValue));

        var (ok, error) = await _surveys.SubmitAsync(id, userId, answers);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The response could not be submitted.");
            var reload = await LoadAsync(id, courseId);
            return reload ?? Page();
        }

        TempData["Message"] = "Response submitted. Thank you!";
        TempData["MessageType"] = "success";
        return RedirectToPage("/Surveys/Open", new { courseId });
    }

    private async Task<IActionResult?> LoadAsync(int id, int? courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var survey = await _surveys.GetAsync(id);
        if (survey is null)
        {
            return NotFound();
        }

        CourseId = courseId;
        Survey = survey;
        WindowClosed = !SurveyService.IsOpen(survey, DateTime.UtcNow);
        AlreadyResponded = await _surveys.HasRespondedAsync(survey, userId);

        if (!AlreadyResponded && !WindowClosed)
        {
            Answers = survey.Questions.OrderBy(q => q.SortOrder)
                .Select(q => new AnswerRow { QuestionId = q.Id })
                .ToList();
        }

        return null;
    }
}
