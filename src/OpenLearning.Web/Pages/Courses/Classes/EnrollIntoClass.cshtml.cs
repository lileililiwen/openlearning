using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.Enrollment.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Web.Pages.Courses.Classes;

[Authorize(Policy = Policies.RequireInstructor)]
public class EnrollIntoClassModel : PageModel
{
    private readonly ClassGroupService _classes;
    private readonly EnrollmentService _enrollments;

    public EnrollIntoClassModel(ClassGroupService classes, EnrollmentService enrollments)
    {
        _classes = classes;
        _enrollments = enrollments;
    }

    public int CourseId { get; set; }

    public List<ClassGroup> Classes { get; set; } = new();

    public List<EnrollmentEntity> Enrollments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _classes.IsCourseOwnerAsync(courseId, userId))
        {
            return Forbid();
        }

        CourseId = courseId;
        Classes = await _classes.GetForCourseAsync(courseId);
        (Enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(courseId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int courseId, int enrollmentId, int? classGroupId)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (classGroupId is null)
        {
            TempData["Message"] = "请选择一个班级。";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { courseId });
        }

        var (ok, error) = await _classes.EnrollIntoClassAsync(classGroupId.Value, enrollmentId, ownerId);
        TempData["Message"] = ok ? "学员已加入班级。" : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }
}
