using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.PeerAssessment.Models;
using OpenLearning.PeerAssessment.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments.PeerReview;

[Authorize]
public class AssessModel : PageModel
{
    private readonly PeerReviewService _peerReview;

    public AssessModel(PeerReviewService peerReview)
    {
        _peerReview = peerReview;
    }

    public PeerAllocationPair? Pair { get; set; }

    public PeerReviewConfig? Config { get; set; }

    public PeerReviewPhase Phase { get; set; }

    public PeerReviewService.AssignmentSubmissionView? Submission { get; set; }

    public List<PeerReviewRubricQuestion> Rubric { get; set; } = new();

    public sealed class AnswerInput
    {
        public int QuestionId { get; set; }

        public int Score { get; set; }

        public string? Comment { get; set; }
    }

    [BindProperty]
    public List<AnswerInput> Answers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int pairId)
    {
        var load = await LoadAsync(pairId);
        return load ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(int pairId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var reviewerPair = await _peerReview.GetPairForReviewerAsync(pairId, userId);
        if (reviewerPair is null)
        {
            return NotFound();
        }

        var answers = Answers.ToDictionary(a => a.QuestionId, a => (a.Score, a.Comment));

        var (ok, error) = await _peerReview.SubmitAssessmentAsync(pairId, userId, answers);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The assessment could not be submitted.");
            var reload = await LoadAsync(pairId);
            return reload ?? Page();
        }

        TempData["Message"] = "Assessment submitted. Thank you!";
        TempData["MessageType"] = "success";
        return RedirectToPage("/Courses/Assignments/PeerReview/MyReviews", new { assignmentId = reviewerPair.AssignmentId });
    }

    private async Task<IActionResult?> LoadAsync(int pairId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var reviewerPair = await _peerReview.GetPairForReviewerAsync(pairId, userId);
        if (reviewerPair is null)
        {
            return NotFound();
        }

        var config = await _peerReview.GetConfigByIdAsync(reviewerPair.Pair.ConfigId);
        if (config is null)
        {
            return NotFound();
        }

        Pair = reviewerPair.Pair;
        Config = config;
        Phase = PeerReviewService.GetPhase(config, DateTime.UtcNow);
        Rubric = config.RubricQuestions.OrderBy(q => q.SortOrder).ToList();

        var queue = await _peerReview.GetReviewerQueueAsync(config.Id, userId);
        Submission = queue.FirstOrDefault(i => i.Pair.Id == pairId)?.Submission;

        if (Submission is null)
        {
            return NotFound();
        }

        return null;
    }
}
