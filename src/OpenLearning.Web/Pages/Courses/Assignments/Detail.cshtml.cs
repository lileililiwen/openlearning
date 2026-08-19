using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assignments.Models;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth;
using OpenLearning.Enrollment.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.Web.Pages.Courses.Assignments;

[Authorize]
public class DetailModel : PageModel
{
    private readonly AssignmentService _assignments;
    private readonly EnrollmentService _enrollments;
    private readonly StorageService _storage;

    public DetailModel(
        AssignmentService assignments,
        EnrollmentService enrollments,
        StorageService storage)
    {
        _assignments = assignments;
        _enrollments = enrollments;
        _storage = storage;
    }

    public class InputModel
    {
        public string Text { get; set; } = string.Empty;

        public IFormFile? File { get; set; }
    }

    public Assignment? Assignment { get; set; }

    public AssignmentSubmission? Submission { get; set; }

    public bool IsOwner { get; set; }

    public bool IsEnrolled { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsOwner = assignment.AuthorId == userId || User.IsInRole(Roles.Admin);
        IsEnrolled = await _enrollments.IsEnrolledAsync(userId, assignment.CourseId);
        if (!IsOwner && !IsEnrolled)
        {
            return Forbid();
        }

        Assignment = assignment;
        if (!IsOwner)
        {
            Submission = await _assignments.GetSubmissionAsync(id, userId);
            if (Submission is not null)
            {
                Input.Text = Submission.Text;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id);
        if (assignment is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsEnrolled = await _enrollments.IsEnrolledAsync(userId, assignment.CourseId);
        if (!IsEnrolled)
        {
            return Forbid();
        }

        if (await _enrollments.IsAccessExpiredAsync(userId, assignment.CourseId))
        {
            TempData["Message"] = "Your access to this course has expired. Please renew to continue learning.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        Assignment = assignment;
        Submission = await _assignments.GetSubmissionAsync(id, userId);

        string? fileUrl = null;
        if (Input.File is not null && Input.File.Length > 0)
        {
            var (file, error) = await _storage.UploadAsync(
                userId, FilePurpose.Assignment, Input.File.FileName, Input.File.ContentType, Input.File.OpenReadStream());
            if (error is not null)
            {
                TempData["Message"] = error;
                TempData["MessageType"] = "danger";
                return RedirectToPage(new { id });
            }

            fileUrl = $"/files/{file!.Key}";
        }

        var (ok, submitError) = await _assignments.SubmitAsync(id, userId, Input.Text, fileUrl);
        TempData["Message"] = ok ? "Submission saved." : submitError;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
