using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Web.Pages.Courses;

public class DetailsModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;

    public DetailsModel(CourseService courses, EnrollmentService enrollments, ProgressService progress)
    {
        _courses = courses;
        _enrollments = enrollments;
        _progress = progress;
    }

    public Course? Course { get; set; }

    public bool IsOwner { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsEnrolled { get; set; }

    public HashSet<int> CompletedLessonIds { get; set; } = new();

    public int ProgressPercent { get; set; }

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

        if (userId is not null)
        {
            IsEnrolled = await _enrollments.IsEnrolledAsync(userId, id);
            if (IsEnrolled)
            {
                CompletedLessonIds = await _progress.GetCompletedLessonIdsAsync(userId, id);
                ProgressPercent = await _progress.GetProgressPercentAsync(userId, id);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEnrollAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
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
