using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly ExamService _exams;

    public CreateModel(ExamService exams)
    {
        _exams = exams;
    }

    [BindProperty]
    public int CourseId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public bool IsOfficial { get; set; }

        [Range(1, 600, ErrorMessage = "Duration must be between 1 and 600 minutes.")]
        public int DurationMinutes { get; set; } = 30;

        [Range(1, 100, ErrorMessage = "Pass percent must be between 1 and 100.")]
        public int PassPercent { get; set; } = 60;

        [Range(1, 50, ErrorMessage = "Max attempts must be between 1 and 50.")]
        public int MaxAttempts { get; set; } = 3;

        public DateTime? OpensAt { get; set; }

        public DateTime? ClosesAt { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        CourseId = courseId;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _exams.IsCourseOwnerAsync(courseId, userId))
        {
            return Forbid();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var draft = new ExamDraft(
            Input.Title,
            Input.Description,
            Input.IsOfficial,
            Input.DurationMinutes,
            Input.PassPercent,
            Input.MaxAttempts,
            NormalizeUtc(Input.OpensAt),
            NormalizeUtc(Input.ClosesAt));

        var exam = await _exams.CreateAsync(CourseId, userId, draft);
        if (exam is null)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Exams/Edit", new { id = exam.Id });
    }

    /// <summary>Date inputs bind as <see cref="DateTimeKind.Unspecified"/>, which Npgsql rejects for timestamptz.</summary>
    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }
}
