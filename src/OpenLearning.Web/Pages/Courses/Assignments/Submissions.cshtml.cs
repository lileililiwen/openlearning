using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments;

[Authorize(Policy = Policies.RequireInstructor)]
public class SubmissionsModel : PageModel
{
    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;
    private readonly UserService _users;
    private readonly NotificationService _notifications;

    public SubmissionsModel(
        AssignmentService assignments,
        CourseService courses,
        UserService users,
        NotificationService notifications)
    {
        _assignments = assignments;
        _courses = courses;
        _users = users;
        _notifications = notifications;
    }

    public Assignment? Assignment { get; set; }

    public List<AssignmentSubmission> Submissions { get; set; } = new();

    public Dictionary<string, string> StudentNames { get; set; } = new();

    public int Ungraded { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(assignment.CourseId);
        if (course is null || (course.InstructorId != userId && !User.IsInRole(Roles.Admin)))
        {
            return Forbid();
        }

        Assignment = assignment;
        Submissions = await _assignments.GetSubmissionsAsync(id);
        Ungraded = Submissions.Count(s => s.GradedAt is null);

        var studentIds = Submissions.Select(s => s.StudentId).Distinct().ToList();
        var users = await _users.GetByIdsAsync(studentIds);
        StudentNames = users
            .Where(u => u is not null)
            .ToDictionary(u => u!.Id, u => u!.DisplayName);

        return Page();
    }

    public async Task<IActionResult> OnPostGradeAsync(int submissionId, int? score, string? feedback)
    {
        var submission = await _assignments.GetSubmissionByIdAsync(submissionId);
        if (submission is null)
        {
            return NotFound();
        }

        var graderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(submission.Assignment.CourseId);
        if (course is null || (course.InstructorId != graderId && !User.IsInRole(Roles.Admin)))
        {
            return Forbid();
        }

        var (ok, error) = await _assignments.GradeAsync(submissionId, graderId, score, feedback);
        if (ok)
        {
            await _notifications.CreateAsync(
                submission.StudentId,
                NotificationType.Quiz,
                $"Assignment graded: {submission.Assignment.Title}",
                score is not null ? $"Your score is {score}/100." : "Your assignment has been graded.",
                $"/Courses/Assignments/Detail?id={submission.AssignmentId}");
        }

        TempData["Message"] = ok ? "Grade saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id = submission.AssignmentId });
    }
}
