using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Surveys.Models;
using OpenLearning.Surveys.Services;

namespace OpenLearning.Web.Pages.Courses.Surveys;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class IndexModel : PageModel
{
    private readonly SurveyService _surveys;
    private readonly CourseService _courses;

    public IndexModel(SurveyService surveys, CourseService courses)
    {
        _surveys = surveys;
        _courses = courses;
    }

    public OpenLearning.CourseManagement.Models.Course? Course { get; set; }

    /// <summary>Null when viewing platform-wide surveys as an Admin.</summary>
    public int? CourseId { get; set; }

    public bool IsPlatformScope { get; set; }

    public List<Survey> Surveys { get; set; } = new();

    [BindProperty] public string Title { get; set; } = string.Empty;

    [BindProperty] public string? Description { get; set; }

    [BindProperty] public bool IsAnonymous { get; set; } = true;

    [BindProperty] public bool AllowLiveResults { get; set; }

    [BindProperty] public DateTime? OpensAt { get; set; }

    [BindProperty] public DateTime? ClosesAt { get; set; }

    [BindProperty] public List<QuestionRow> Questions { get; set; } = new();

    public sealed class QuestionRow
    {
        public SurveyQuestionType Type { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = true;

        public string Options { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var load = await LoadAsync(courseId);
        return load ?? Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var platformScope = courseId == 0;

        if (!platformScope)
        {
            var course = await _courses.GetByIdAsync(courseId);
            if (course is null)
            {
                return NotFound();
            }

            if (course.InstructorId != userId && !isAdmin)
            {
                return Forbid();
            }
        }
        else if (!isAdmin)
        {
            return Forbid();
        }

        var inputs = Questions
            .Where(q => !string.IsNullOrWhiteSpace(q.Prompt))
            .Select(q => new SurveyService.QuestionInput(
                q.Type,
                q.Prompt,
                q.IsRequired,
                q.Options.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)))
            .ToList();

        var (ok, error) = await _surveys.CreateAsync(
            userId,
            isAdmin,
            platformScope ? SurveyScope.Platform : SurveyScope.Course,
            platformScope ? null : courseId,
            Title,
            Description ?? string.Empty,
            IsAnonymous,
            AllowLiveResults,
            OpensAt,
            ClosesAt,
            inputs);

        TempData["Message"] = ok ? "Survey created." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    private async Task<IActionResult?> LoadAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);

        if (courseId == 0)
        {
            if (!isAdmin)
            {
                return Forbid();
            }

            IsPlatformScope = true;
            CourseId = null;
            Surveys = await _surveys.GetPlatformSurveysAsync();
        }
        else
        {
            var course = await _courses.GetByIdAsync(courseId);
            if (course is null)
            {
                return NotFound();
            }

            if (course.InstructorId != userId && !isAdmin)
            {
                return Forbid();
            }

            Course = course;
            CourseId = courseId;
            Surveys = await _surveys.GetForCourseAsync(courseId);
        }

        while (Questions.Count < 10)
        {
            Questions.Add(new QuestionRow());
        }

        return null;
    }
}
