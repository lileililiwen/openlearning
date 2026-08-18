using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Web.Pages;

[Authorize(Policy = Policies.RequireStudent)]
public class MyCoursesModel : PageModel
{
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;

    public MyCoursesModel(EnrollmentService enrollments, ProgressService progress)
    {
        _enrollments = enrollments;
        _progress = progress;
    }

    public record EnrolledCourse(EnrollmentEntity Enrollment, int ProgressPercent);

    public List<EnrolledCourse> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var enrollments = await _enrollments.GetStudentEnrollmentsAsync(userId);

        var courses = new List<EnrolledCourse>();
        foreach (var enrollment in enrollments)
        {
            var percent = await _progress.GetProgressPercentAsync(userId, enrollment.CourseId);
            courses.Add(new EnrolledCourse(enrollment, percent));
        }

        Courses = courses;
    }

    public async Task<IActionResult> OnPostWithdrawAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _enrollments.WithdrawAsync(userId, id);
        return RedirectToPage();
    }
}
