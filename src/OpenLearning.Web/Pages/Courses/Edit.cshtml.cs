using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Logging.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly CourseService _courses;
    private readonly QuizService _quizzes;
    private readonly AnnouncementService _announcements;
    private readonly LogService _logs;
    private readonly CategoryService _categories;

    public EditModel(CourseService courses, QuizService quizzes, AnnouncementService announcements, LogService logs, CategoryService categories)
    {
        _courses = courses;
        _quizzes = quizzes;
        _announcements = announcements;
        _logs = logs;
        _categories = categories;
    }

    public Course? Course { get; set; }

    public List<Quiz> Quizzes { get; set; } = new();

    public List<CourseAnnouncement> Announcements { get; set; } = new();

    public List<Category> Categories { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? AnnouncementBody { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        [Range(0, 99999, ErrorMessage = "Price must be between 0 and 99999.")]
        [Display(Name = "Price (leave blank for free)")]
        public decimal? Price { get; set; }

        [Display(Name = "Level")]
        public CourseLevel? Level { get; set; }

        [StringLength(50)]
        [Display(Name = "Duration (e.g. \"6 hours\")")]
        public string Duration { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Language")]
        public string Language { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        [StringLength(2000)]
        [Display(Name = "Prerequisites")]
        public string Prerequisites { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        [StringLength(2000)]
        [Display(Name = "What students will learn")]
        public string LearningOutcomes { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Tags (comma-separated)")]
        public string Tags { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        if (course.InstructorId != userId)
        {
            return Forbid();
        }

        Course = course;
        Quizzes = await _quizzes.GetForCourseAsync(id);
        Announcements = await _announcements.ListForCourseAsync(id);
        Categories = await _categories.GetActiveAsync();
        Input.Title = course.Title;
        Input.Description = course.Description;
        Input.Category = course.Category;
        Input.Price = course.Price;
        Input.Level = course.Level;
        Input.Duration = course.Duration;
        Input.Language = course.Language;
        Input.Prerequisites = course.Prerequisites;
        Input.LearningOutcomes = course.LearningOutcomes;
        Input.Tags = string.Join(", ", course.Tags.Select(ct => ct.Tag.Name));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course is not null)
            {
                Course = course;
                Quizzes = await _quizzes.GetForCourseAsync(id);
                Announcements = await _announcements.ListForCourseAsync(id);
                Categories = await _categories.GetActiveAsync();
            }

            return Page();
        }

        var updated = await _courses.UpdateAsync(
            id,
            userId,
            Input.Title,
            Input.Description,
            Input.Category,
            Input.Price,
            Input.Level,
            Input.Duration,
            Input.Language,
            Input.Prerequisites,
            Input.LearningOutcomes,
            SplitTags(Input.Tags));
        if (!updated)
        {
            return Forbid();
        }

        return RedirectToPage(new { id });
    }

    private static string[] SplitTags(string tags)
    {
        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task<IActionResult> OnPostAnnounceAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _announcements.PostAsync(id, userId, AnnouncementBody ?? string.Empty);
        if (ok)
        {
            await _logs.RecordAsync(
                userId,
                User.Identity?.Name ?? string.Empty,
                "PostAnnouncement",
                "Course",
                id.ToString(CultureInfo.InvariantCulture),
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ok ? "Announcement posted to enrolled students." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
