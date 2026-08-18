using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Models;
using OpenLearning.Chat.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Web.Pages.Courses;

public class ChatModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly ChatService _chat;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatModel(
        CourseService courses,
        EnrollmentService enrollments,
        ChatService chat,
        UserManager<ApplicationUser> userManager)
    {
        _courses = courses;
        _enrollments = enrollments;
        _chat = chat;
        _userManager = userManager;
    }

    public Course? Course { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user?.IsSuspended == true)
        {
            return Forbid();
        }

        var isOwner = course.InstructorId == userId;
        var isAdmin = User.IsInRole(Roles.Admin);
        if (course.Status == CourseStatus.Draft && !isOwner && !isAdmin)
        {
            return Forbid();
        }

        var isEnrolled = await _enrollments.IsEnrolledAsync(userId, id);
        if (course.IsPublished && !isOwner && !isAdmin && !isEnrolled)
        {
            return Forbid();
        }

        Course = course;
        Messages = await _chat.GetRecentMessagesAsync(id);
        return Page();
    }
}
