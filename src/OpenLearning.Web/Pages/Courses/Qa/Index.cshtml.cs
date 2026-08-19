using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Community.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Web.Pages.Courses.Qa;

public class IndexModel : PageModel
{
    private readonly CommunityService _community;
    private readonly EnrollmentService _enrollments;
    private readonly CourseService _courses;

    public IndexModel(CommunityService community, EnrollmentService enrollments, CourseService courses)
    {
        _community = community;
        _enrollments = enrollments;
        _courses = courses;
    }

    public Course? Course { get; set; }

    public List<OpenLearning.Community.Models.Question> Questions { get; set; } = new();

    public bool CanAccess { get; set; }

    public bool IsOwner { get; set; }

    public bool IsAdmin { get; set; }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    public QuestionInput Input { get; set; } = new();

    public class QuestionInput
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Body { get; set; } = string.Empty;
    }

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
        CanAccess = IsOwner || IsAdmin ||
            (userId is not null && await _enrollments.IsEnrolledAsync(userId, id));

        if (!CanAccess)
        {
            return Forbid();
        }

        Course = course;
        Questions = await _community.GetQuestionsAsync(id, userId, IsAdmin);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!ModelState.IsValid)
        {
            return RedirectToPage(new { id = CourseId });
        }

        var (ok, error) = await _community.AskAsync(CourseId, userId, Input.Title, Input.Body, null, isAdmin);
        TempData["Message"] = ok ? "问题已发布。" : error;
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostReplyAsync(int questionId, string replyBody)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (ok, error) = await _community.ReplyToQuestionAsync(questionId, userId, replyBody ?? string.Empty, isAdmin);
        TempData["Message"] = ok ? "回复已发布。" : error;
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int questionId)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        await _community.DeleteQuestionAsync(questionId);
        return RedirectToPage(new { id = CourseId });
    }
}
