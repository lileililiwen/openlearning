using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Chat.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;
using OpenLearning.Storage.Services;

namespace OpenLearning.Web.Pages.Courses.Live;

public class RoomModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly LiveService _live;
    private readonly StorageService _storage;

    public RoomModel(
        CourseService courses,
        EnrollmentService enrollments,
        LiveService live,
        StorageService storage)
    {
        _courses = courses;
        _enrollments = enrollments;
        _live = live;
        _storage = storage;
    }

    public LiveSession? Session { get; set; }

    public Course? Course { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();

    public bool CanManage { get; set; }

    public bool IsEnrolled { get; set; }

    public bool HasCheckedIn { get; set; }

    public bool InCheckInWindow { get; set; }

    public string? ReplayUrl { get; set; }

    public string? CurrentUserId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var session = await _live.GetByIdAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        var course = await _courses.GetByIdAsync(session.CourseId);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole(Roles.Admin);
        var isOwner = session.InstructorId == userId;
        var isCoHost = await _live.IsCoHostAsync(id, userId);
        var isEnrolled = await _enrollments.IsEnrolledAsync(userId, session.CourseId);
        if (!isAdmin && !isOwner && !isCoHost && !isEnrolled)
        {
            return Forbid();
        }

        Session = session;
        Course = course;
        CurrentUserId = userId;
        CanManage = isOwner || isCoHost || isAdmin;
        IsEnrolled = isEnrolled;
        HasCheckedIn = await _live.HasCheckedInAsync(id, userId);

        var now = DateTime.UtcNow;
        InCheckInWindow = session.Status == LiveSessionStatus.Live
            && now >= session.StartsAt
            && now <= session.EndsAt;

        Messages = await _live.GetLiveMessagesAsync(id);

        if (session.RecordingFileId is int fileId)
        {
            var file = await _storage.GetByIdAsync(fileId);
            if (file is not null)
            {
                ReplayUrl = $"/files/{file.Key}";
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCheckInAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.CheckInAsync(id, userId);
        TempData["Message"] = ok ? "You're checked in. Enjoy the session!" : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostStartAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.StartAsync(id, userId);
        TempData["Message"] = ok ? "Session is now live." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEndAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.EndAsync(id, userId, null);
        TempData["Message"] = ok ? "Session ended." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAttachRecordingAsync(int id, int fileId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _live.EndAsync(id, userId, fileId);
        TempData["Message"] = ok ? "Recording attached and session ended." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
