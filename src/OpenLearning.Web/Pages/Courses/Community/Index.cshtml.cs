using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Community.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Moderation.Models;
using OpenLearning.Moderation.Services;

namespace OpenLearning.Web.Pages.Courses.Community;

public class IndexModel : PageModel
{
    private readonly CommunityService _community;
    private readonly EnrollmentService _enrollments;
    private readonly CourseService _courses;
    private readonly ContentReviewService _contentReview;

    public IndexModel(CommunityService community, EnrollmentService enrollments, CourseService courses, ContentReviewService contentReview)
    {
        _community = community;
        _enrollments = enrollments;
        _courses = courses;
        _contentReview = contentReview;
    }

    public Course? Course { get; set; }

    public List<OpenLearning.Community.Models.Post> Posts { get; set; } = new();

    public bool CanAccess { get; set; }

    public bool IsOwner { get; set; }

    public bool IsAdmin { get; set; }

    public string? CurrentUserId { get; set; }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        CurrentUserId = userId;
        IsOwner = userId is not null && course.InstructorId == userId;
        IsAdmin = User.IsInRole(Roles.Admin);
        CanAccess = IsOwner || IsAdmin ||
            (userId is not null && await _enrollments.IsEnrolledAsync(userId, id));

        if (!CanAccess)
        {
            return Forbid();
        }

        Course = course;
        Posts = await _community.GetPostsAsync(id, userId, IsAdmin);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (ok, error) = await _community.CreatePostAsync(CourseId, userId, Body, null, isAdmin);
        TempData["Message"] = ok ? "动态已发布。" : error;
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostReplyAsync(int postId, string replyBody)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (ok, error) = await _community.ReplyToPostAsync(postId, userId, replyBody ?? string.Empty, isAdmin);
        TempData["Message"] = ok ? "回复已发布。" : error;
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int postId)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        await _community.DeletePostAsync(postId);
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostReportAsync(int courseId, string contentType, int contentId, string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var type = Enum.TryParse<ReportedContentType>(contentType, out var parsed)
            ? parsed
            : ReportedContentType.Post;
        var (ok, error) = await _contentReview.ReportAsync(userId, type, contentId, reason ?? string.Empty);
        TempData["Message"] = ok ? "感谢举报，管理员将尽快处理。" : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id = courseId });
    }
}
