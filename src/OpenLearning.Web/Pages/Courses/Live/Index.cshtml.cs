using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages.Courses.Live;

public class IndexModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly LiveService _live;

    public IndexModel(CourseService courses, EnrollmentService enrollments, LiveService live)
    {
        _courses = courses;
        _enrollments = enrollments;
        _live = live;
    }

    public Course? Course { get; set; }

    public List<LiveSession> Sessions { get; set; } = new();

    public List<int> ManageableSessionIds { get; set; } = new();

    public bool IsOwner { get; set; }

    public bool IsAdmin { get; set; }

    public bool CanAccess { get; set; }

    public string? CurrentUserId { get; set; }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    public LiveInputModel Input { get; set; } = new();

    public class LiveInputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartsAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime EndsAt { get; set; } = DateTime.Now.AddHours(1);
    }

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
        Sessions = await _live.GetForCourseAsync(id);

        if (userId is not null)
        {
            var checks = Sessions.Select(s => _live.CanManageAsync(s.Id, userId));
            var results = await Task.WhenAll(checks);
            ManageableSessionIds = Sessions.Zip(results, (session, ok) => (session.Id, ok))
                .Where(x => x.ok)
                .Select(x => x.Id)
                .ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToPage(new { id = CourseId });
        }

        var input = new LiveInput(
            Input.Title,
            Input.Description,
            DateTime.SpecifyKind(Input.StartsAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(Input.EndsAt, DateTimeKind.Utc));
        var (ok, error) = await _live.CreateAsync(CourseId, userId, input);
        TempData["Message"] = ok ? "Live session scheduled." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        await _live.DeleteAsync(sessionId, userId);
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostStartAsync(int sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.StartAsync(sessionId, userId);
        TempData["Message"] = ok ? "Session is now live." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostEndAsync(int sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.EndAsync(sessionId, userId, null);
        TempData["Message"] = ok ? "Session ended." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostAddCoHostAsync(int sessionId, string email)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.AddCoHostAsync(sessionId, userId, email ?? string.Empty);
        TempData["Message"] = ok ? "Co-host invited." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id = CourseId });
    }

    public async Task<IActionResult> OnPostRemoveCoHostAsync(int sessionId, string coHostId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        await _live.RemoveCoHostAsync(sessionId, userId, coHostId);
        return RedirectToPage(new { id = CourseId });
    }
}
