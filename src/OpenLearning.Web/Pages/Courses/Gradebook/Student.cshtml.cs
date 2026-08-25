using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Enrollment.Services;
using OpenLearning.Gradebook.Models;
using OpenLearning.Gradebook.Services;

namespace OpenLearning.Web.Pages.Courses.Gradebook;

[Authorize]
public class StudentModel : PageModel
{
    private readonly GradebookService _gradebook;
    private readonly EnrollmentService _enrollments;

    public StudentModel(GradebookService gradebook, EnrollmentService enrollments)
    {
        _gradebook = gradebook;
        _enrollments = enrollments;
    }

    public int CourseId { get; set; }

    public GradebookConfig? Config { get; set; }

    public GradebookService.StudentAggregate? MyRow { get; set; }

    public GradebookSnapshot? Snapshot { get; set; }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var config = await _gradebook.GetConfigAsync(courseId);
        if (config is null || !config.IsPublished)
        {
            return NotFound();
        }

        if (!await _enrollments.IsEnrolledAsync(userId, courseId))
        {
            return Forbid();
        }

        CourseId = courseId;
        Config = config;

        var rows = await _gradebook.ComputeAsync(config);
        MyRow = rows.FirstOrDefault(r => r.StudentId == userId);
        Snapshot = await _gradebook.GetSnapshotAsync(config.Id, userId);

        return Page();
    }
}
