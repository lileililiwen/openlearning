using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.CourseManagement.Services;
using OpenLearning.PeerAssessment.Models;
using OpenLearning.PeerAssessment.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments.PeerReview;

[Authorize(Policy = Policies.RequireInstructor)]
public class ManageModel : PageModel
{
    private readonly PeerReviewService _peerReview;
    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;
    private readonly UserService _users;

    public ManageModel(
        PeerReviewService peerReview,
        AssignmentService assignments,
        CourseService courses,
        UserService users)
    {
        _peerReview = peerReview;
        _assignments = assignments;
        _courses = courses;
        _users = users;
    }

    public OpenLearning.Assignments.Models.Assignment? Assignment { get; set; }

    public PeerReviewConfig? Config { get; set; }

    public PeerReviewPhase Phase { get; set; }

    public PeerAllocationRun? LatestRun { get; set; }

    public int ActivePairCount { get; set; }

    public int AssessmentCount { get; set; }

    public int ParticipantCount { get; set; }

    public List<PeerReviewResult> Results { get; set; } = new();

    public Dictionary<string, string> StudentNames { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int assignmentId)
    {
        var load = await LoadAsync(assignmentId);
        return load ?? Page();
    }

    public async Task<IActionResult> OnPostAllocateAsync(int assignmentId)
    {
        var config = await AuthorizeAsync(assignmentId);
        if (config is null)
        {
            return RejectAsync(assignmentId, "Peer review is not configured for this assignment.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _peerReview.RunAllocationAsync(config, userId);
        TempData["Message"] = ok ? "Reviewer allocation completed." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { assignmentId });
    }

    public async Task<IActionResult> OnPostReleaseAsync(int assignmentId)
    {
        var config = await AuthorizeAsync(assignmentId);
        if (config is null)
        {
            return RejectAsync(assignmentId, "Peer review is not configured for this assignment.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _peerReview.ReleaseResultsAsync(config, userId);
        TempData["Message"] = ok ? "Results released to students." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { assignmentId });
    }

    public async Task<IActionResult> OnPostOverrideAsync(
        int assignmentId, string studentId, int? overrideScore)
    {
        var config = await AuthorizeAsync(assignmentId);
        if (config is null)
        {
            return RejectAsync(assignmentId, "Peer review is not configured for this assignment.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _peerReview.SetOverrideAsync(config.Id, studentId, overrideScore, userId);
        TempData["Message"] = ok ? "Override saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { assignmentId });
    }

    private async Task<PeerReviewConfig?> AuthorizeAsync(int assignmentId)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
        {
            return null;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(assignment.CourseId);
        if (course is null || (course.InstructorId != userId && !User.IsInRole(Roles.Admin)))
        {
            return null;
        }

        return await _peerReview.GetConfigAsync(assignmentId);
    }

    private RedirectToPageResult RejectAsync(int assignmentId, string message)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = "danger";
        return RedirectToPage(new { assignmentId });
    }

    private async Task<IActionResult?> LoadAsync(int assignmentId)
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

        var config = await _peerReview.GetConfigAsync(assignmentId);
        if (config is null)
        {
            return RedirectToPage("./Configure", new { assignmentId });
        }

        await _peerReview.EnsureAllocatedAsync(config);

        Assignment = assignment;
        Config = config;
        Phase = PeerReviewService.GetPhase(config, DateTime.UtcNow);
        LatestRun = await _peerReview.GetLatestRunAsync(config.Id);
        ActivePairCount = await _peerReview.CountActivePairsAsync(config.Id);
        ParticipantCount = await _peerReview.CountParticipantsAsync(config);
        AssessmentCount = await _peerReview.CountAssessmentsAsync(config.Id);

        Results = await _peerReview.GetInstructorResultsAsync(config.Id);
        var names = await _users.GetByIdsAsync(Results.Select(r => r.StudentId));
        StudentNames = names
            .Where(u => u is not null)
            .ToDictionary(u => u!.Id, u => u!.DisplayName);

        return null;
    }
}
