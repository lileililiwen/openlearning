using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Services;
using OpenLearning.PeerAssessment.Models;
using OpenLearning.PeerAssessment.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments.PeerReview;

[Authorize]
public class MyReviewsModel : PageModel
{
    private readonly PeerReviewService _peerReview;
    private readonly AssignmentService _assignments;

    public MyReviewsModel(PeerReviewService peerReview, AssignmentService assignments)
    {
        _peerReview = peerReview;
        _assignments = assignments;
    }

    public OpenLearning.Assignments.Models.Assignment? Assignment { get; set; }

    public PeerReviewConfig? Config { get; set; }

    public PeerReviewPhase Phase { get; set; }

    public List<PeerReviewService.ReviewQueueItem> Queue { get; set; } = new();

    public List<PeerReviewService.ReceivedAssessment> Received { get; set; } = new();

    public int RubricMax { get; set; }

    public PeerReviewResult? MyResult { get; set; }

    public async Task<IActionResult> OnGetAsync(int assignmentId)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
        {
            return NotFound();
        }

        var config = await _peerReview.GetConfigAsync(assignmentId);
        if (config is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Assignment = assignment;
        Config = config;
        Phase = PeerReviewService.GetPhase(config, DateTime.UtcNow);

        await _peerReview.EnsureAllocatedAsync(config);
        Queue = await _peerReview.GetReviewerQueueAsync(config.Id, userId);

        if (config.ResultsReleasedAt is not null)
        {
            (Received, RubricMax) = await _peerReview.GetReceivedAssessmentsAsync(config, userId);
            MyResult = await _peerReview.GetMyResultAsync(config.Id, userId);
        }

        return Page();
    }
}
