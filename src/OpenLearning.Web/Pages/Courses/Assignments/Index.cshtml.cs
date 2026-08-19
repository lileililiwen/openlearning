using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AssignmentService _assignments;
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;

    public IndexModel(
        AssignmentService assignments,
        CourseService courses,
        EnrollmentService enrollments)
    {
        _assignments = assignments;
        _courses = courses;
        _enrollments = enrollments;
    }

    public Course? Course { get; set; }

    public List<Assignment> Assignments { get; set; } = new();

    public Dictionary<int, AssignmentSubmission?> Submissions { get; set; } = new();

    public Dictionary<int, int> UngradedCounts { get; set; } = new();

    public bool IsOwner { get; set; }

    public bool IsEnrolled { get; set; }

    /// <summary>Button label for the student's submission state.</summary>
    public static string ActionLabel(int assignmentId, AssignmentSubmission? submission)
    {
        if (submission is null)
        {
            return "Submit";
        }

        return submission.GradedAt is null ? "Resubmit" : "View result";
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var course = await _courses.GetByIdAsync(courseId);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsOwner = course.InstructorId == userId || User.IsInRole(Roles.Admin);
        IsEnrolled = await _enrollments.IsEnrolledAsync(userId, courseId);

        // Only enrolled students and the owner may see the assignment list.
        if (!IsOwner && !IsEnrolled)
        {
            return Forbid();
        }

        Course = course;
        Assignments = await _assignments.GetForCourseAsync(courseId);

        if (!IsOwner)
        {
            foreach (var assignmentId in Assignments.Select(a => a.Id))
            {
                Submissions[assignmentId] = await _assignments.GetSubmissionAsync(assignmentId, userId);
            }
        }

        UngradedCounts = new Dictionary<int, int>();
        foreach (var assignmentId in Assignments.Select(a => a.Id))
        {
            var subs = await _assignments.GetSubmissionsAsync(assignmentId);
            UngradedCounts[assignmentId] = subs.Count(s => s.GradedAt is null);
        }

        return Page();
    }
}
