using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly ExamService _exams;

    public EditModel(ExamService exams)
    {
        _exams = exams;
    }

    public Exam? Exam { get; set; }

    [BindProperty]
    public int Id { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var exam = await _exams.GetByIdAsync(id);
        if (exam is null)
        {
            return NotFound();
        }

        if (exam.Course is null || exam.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Exam = exam;
        Id = id;
        Input.Title = exam.Title;
        Input.Description = exam.Description;
        Input.IsOfficial = exam.IsOfficial;
        Input.DurationMinutes = exam.DurationMinutes;
        Input.PassPercent = exam.PassPercent;
        Input.MaxAttempts = exam.MaxAttempts;
        Input.OpensAt = exam.OpensAt;
        Input.ClosesAt = exam.ClosesAt;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            Exam = await _exams.GetByIdAsync(Id);
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

        var updated = await _exams.UpdateAsync(Id, userId, draft);
        if (!updated)
        {
            return Forbid();
        }

        return RedirectToPage(new { id = Id });
    }

    /// <summary>Date inputs bind as <see cref="DateTimeKind.Unspecified"/>, which Npgsql rejects for timestamptz.</summary>
    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }
}
