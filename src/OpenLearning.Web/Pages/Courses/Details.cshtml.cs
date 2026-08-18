using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.Certificates.Models;
using OpenLearning.Certificates.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Ratings.Models;
using OpenLearning.Ratings.Services;

namespace OpenLearning.Web.Pages.Courses;

public class DetailsModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly QuizService _quizzes;
    private readonly OrderService _orders;
    private readonly ReviewService _reviews;
    private readonly CertificateService _certificates;

    public DetailsModel(
        CourseService courses,
        EnrollmentService enrollments,
        ProgressService progress,
        QuizService quizzes,
        OrderService orders,
        ReviewService reviews,
        CertificateService certificates)
    {
        _courses = courses;
        _enrollments = enrollments;
        _progress = progress;
        _quizzes = quizzes;
        _orders = orders;
        _reviews = reviews;
        _certificates = certificates;
    }

    public class ReviewInputModel
    {
        [Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5 stars.")]
        public int Rating { get; set; }

        [StringLength(2000, ErrorMessage = "Comment must be 2000 characters or fewer.")]
        public string? Comment { get; set; }
    }

    public Course? Course { get; set; }

    public List<Quiz> Quizzes { get; set; } = new();

    public bool IsOwner { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsEnrolled { get; set; }

    public bool HasPaidOrder { get; set; }

    public HashSet<int> CompletedLessonIds { get; set; } = new();

    public int ProgressPercent { get; set; }

    public RatingAggregate Aggregate { get; set; } = new(0d, 0);

    public List<ReviewWithAuthor> PublicReviews { get; set; } = new();

    public Review? UserReview { get; set; }

    public Certificate? Certificate { get; set; }

    [BindProperty]
    public ReviewInputModel ReviewInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwner = userId is not null && course.InstructorId == userId;
        IsAdmin = User.IsInRole(Roles.Admin);

        if (course.Status == CourseStatus.Draft && !IsOwner && !IsAdmin)
        {
            return Forbid();
        }

        Course = course;
        Quizzes = await _quizzes.GetForCourseAsync(id);
        Aggregate = await _reviews.GetRatingAsync(id);

        if (userId is not null)
        {
            IsEnrolled = await _enrollments.IsEnrolledAsync(userId, id);
            if (course.Price is > 0)
            {
                HasPaidOrder = await _orders.HasPaidOrderAsync(userId, id);
            }

            if (IsEnrolled)
            {
                CompletedLessonIds = await _progress.GetCompletedLessonIdsAsync(userId, id);
                ProgressPercent = await _progress.GetProgressPercentAsync(userId, id);
                Certificate = await _certificates.EnsureIssuedAsync(userId, id);
                UserReview = await _reviews.GetUserReviewAsync(userId, id);
                if (UserReview is not null)
                {
                    ReviewInput = new ReviewInputModel
                    {
                        Rating = UserReview.Rating,
                        Comment = UserReview.Comment,
                    };
                }
            }
        }

        // Owner sees all reviews; public visitors see up to 5 most recent.
        var allReviews = await _reviews.GetReviewsForCourseAsync(id);
        PublicReviews = IsOwner || IsAdmin
            ? allReviews
            : allReviews.Take(5).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostEnrollAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        // Paid courses cannot be enrolled directly without a paid order.
        if (course.Price is > 0 && !await _orders.HasPaidOrderAsync(userId, id))
        {
            TempData["Message"] = "This course requires purchase before enrollment.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        var (ok, error) = await _enrollments.EnrollAsync(userId, id);
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostWithdrawAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        await _enrollments.WithdrawAsync(userId, id);
        return RedirectToPage("/MyCourses");
    }

    public async Task<IActionResult> OnPostSubmitReviewAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _enrollments.IsEnrolledAsync(userId, id))
        {
            TempData["Message"] = "You must be enrolled in this course to leave a review.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        var (ok, error) = await _reviews.SubmitAsync(userId, id, ReviewInput.Rating, ReviewInput.Comment);
        TempData["Message"] = ok ? "Thanks for your review!" : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPublishAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var newStatus = course.IsPublished ? CourseStatus.Draft : CourseStatus.Published;
        await _courses.SetStatusAsync(id, userId, newStatus);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var deleted = User.IsInRole(Roles.Admin)
            ? await _courses.DeleteAnyAsync(id)
            : await _courses.DeleteAsync(id, userId);

        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Manage");
    }
}
