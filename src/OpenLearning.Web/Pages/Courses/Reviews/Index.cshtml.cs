using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Moderation.Models;
using OpenLearning.Moderation.Services;
using OpenLearning.Ratings.Services;

namespace OpenLearning.Web.Pages.Courses.Reviews;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class IndexModel : PageModel
{
    private readonly CourseService _courses;
    private readonly ReviewService _reviews;
    private readonly ContentReviewService _contentReview;

    public IndexModel(CourseService courses, ReviewService reviews, ContentReviewService contentReview)
    {
        _courses = courses;
        _reviews = reviews;
        _contentReview = contentReview;
    }

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public List<ReviewWithAuthor> Reviews { get; set; } = new();

    public RatingAggregate Aggregate { get; set; } = new(0d, 0);

    public string? CurrentUserId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        CurrentUserId = userId;
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!isAdmin && !await _courses.IsOwnerAsync(id, userId))
        {
            return Forbid();
        }

        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        CourseId = id;
        CourseTitle = course.Title;
        Reviews = await _reviews.GetReviewsForCourseAsync(id);
        Aggregate = await _reviews.GetRatingAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id, int reviewId)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        var ok = await _reviews.DeleteAsync(reviewId);
        TempData["Message"] = ok ? "Review removed." : "Review not found.";
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReportAsync(int id, string contentType, int contentId, string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var type = Enum.TryParse<ReportedContentType>(contentType, out var parsed)
            ? parsed
            : ReportedContentType.Review;
        var (ok, error) = await _contentReview.ReportAsync(userId, type, contentId, reason ?? string.Empty);
        TempData["Message"] = ok ? "Thanks — your report was submitted for review." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
