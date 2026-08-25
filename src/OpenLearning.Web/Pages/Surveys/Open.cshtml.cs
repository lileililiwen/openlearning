using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Surveys.Models;
using OpenLearning.Surveys.Services;

namespace OpenLearning.Web.Pages.Surveys;

[Authorize]
public class OpenModel : PageModel
{
    private readonly SurveyService _surveys;

    public OpenModel(SurveyService surveys)
    {
        _surveys = surveys;
    }

    public List<Survey> Available { get; set; } = new();

    public HashSet<int> Responded { get; set; } = new();

    public int? CourseId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        CourseId = courseId;

        var candidates = courseId is int cid
            ? await _surveys.GetForCourseAsync(cid)
            : await _surveys.GetPlatformSurveysAsync();

        foreach (var survey in candidates)
        {
            if (!SurveyService.IsOpen(survey, DateTime.UtcNow))
            {
                continue;
            }

            if (!await _surveys.IsEligibleAsync(survey, userId))
            {
                continue;
            }

            Available.Add(survey);
            if (await _surveys.HasRespondedAsync(survey, userId))
            {
                Responded.Add(survey.Id);
            }
        }

        return Page();
    }
}
