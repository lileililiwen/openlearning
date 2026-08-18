using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Ratings.Services;

namespace OpenLearning.Web.Pages.Courses.Reviews;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public class IndexModel : PageModel
{
    private readonly CourseService _courses;
    private readonly ReviewService _reviews;

    public IndexModel(CourseService courses, ReviewService reviews)
    {
        _courses = courses;
        _reviews = reviews;
    }

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public List<ReviewWithAuthor> Reviews { get; set; } = new();

    public RatingAggregate Aggregate { get; set; } = new(0d, 0);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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
}
