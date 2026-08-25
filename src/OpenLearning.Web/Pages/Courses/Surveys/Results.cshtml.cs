using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Surveys.Models;
using OpenLearning.Surveys.Services;

namespace OpenLearning.Web.Pages.Courses.Surveys;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class ResultsModel : PageModel
{
    private readonly SurveyService _surveys;

    public ResultsModel(SurveyService surveys)
    {
        _surveys = surveys;
    }

    public Survey? Survey { get; set; }

    public SurveyService.SurveyResults? Results { get; set; }

    public int BackCourseId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, int courseId)
    {
        var survey = await _surveys.GetAsync(id);
        if (survey is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _surveys.CanManageAsync(survey, userId, User.IsInRole(Roles.Admin)))
        {
            return Forbid();
        }

        Survey = survey;
        BackCourseId = courseId;
        Results = await _surveys.GetResultsAsync(survey, userId, User.IsInRole(Roles.Admin));
        return Page();
    }
}
