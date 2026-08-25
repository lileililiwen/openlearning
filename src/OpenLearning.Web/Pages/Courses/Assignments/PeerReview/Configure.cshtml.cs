using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.PeerAssessment.Models;
using OpenLearning.PeerAssessment.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments.PeerReview;

[Authorize(Policy = Policies.RequireInstructor)]
public class ConfigureModel : PageModel
{
    private readonly PeerReviewService _peerReview;
    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;

    public ConfigureModel(
        PeerReviewService peerReview,
        AssignmentService assignments,
        CourseService courses)
    {
        _peerReview = peerReview;
        _assignments = assignments;
        _courses = courses;
    }

    public sealed class RubricRowInput
    {
        public string Prompt { get; set; } = string.Empty;

        public int MaxPoints { get; set; } = 10;
    }

    public sealed class ConfigInput
    {
        public int ReviewsPerStudent { get; set; } = 3;

        public bool IsAnonymous { get; set; } = true;

        public PeerReviewStrategy Strategy { get; set; } = PeerReviewStrategy.WeightedMix;

        public int InstructorWeightPercent { get; set; } = 60;

        public DateTime? ReviewOpensAt { get; set; }

        public DateTime? ReviewClosesAt { get; set; }

        public List<RubricRowInput> Rubric { get; set; } = new();
    }

    public OpenLearning.Assignments.Models.Assignment? Assignment { get; set; }

    public PeerReviewConfig? Config { get; set; }

    public bool Locked { get; set; }

    [BindProperty]
    public ConfigInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int assignmentId)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(assignment.CourseId);
        if (course is null || (course.InstructorId != userId && !User.IsInRole(Roles.Admin)))
        {
            return Forbid();
        }

        Assignment = assignment;
        Config = await _peerReview.GetConfigAsync(assignmentId);
        Locked = Config is not null &&
                 PeerReviewService.GetPhase(Config, DateTime.UtcNow) != PeerReviewPhase.Submission;

        if (Config is not null)
        {
            Input.ReviewsPerStudent = Config.ReviewsPerStudent;
            Input.IsAnonymous = Config.IsAnonymous;
            Input.Strategy = Config.Strategy;
            Input.InstructorWeightPercent = Config.InstructorWeightPercent;
            Input.ReviewOpensAt = Config.ReviewOpensAt;
            Input.ReviewClosesAt = Config.ReviewClosesAt;
            Input.Rubric = Config.RubricQuestions
                .OrderBy(q => q.SortOrder)
                .Select(q => new RubricRowInput { Prompt = q.Prompt, MaxPoints = q.MaxPoints })
                .ToList();
        }

        while (Input.Rubric.Count < 10)
        {
            Input.Rubric.Add(new RubricRowInput());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int assignmentId)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(assignment.CourseId);
        if (course is null || (course.InstructorId != userId && !User.IsInRole(Roles.Admin)))
        {
            return Forbid();
        }

        var rubric = Input.Rubric
            .Where(r => !string.IsNullOrWhiteSpace(r.Prompt))
            .Select(r => (r.Prompt.Trim(), r.MaxPoints))
            .ToList();

        var (ok, error) = await _peerReview.SaveConfigAsync(
            assignmentId,
            Input.ReviewsPerStudent,
            Input.IsAnonymous,
            Input.Strategy,
            Input.InstructorWeightPercent,
            Input.ReviewOpensAt,
            Input.ReviewClosesAt ?? DateTime.UtcNow.AddDays(7),
            rubric);

        TempData["Message"] = ok ? "Peer review settings saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { assignmentId });
    }
}
