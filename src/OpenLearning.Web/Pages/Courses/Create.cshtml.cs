using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly CourseService _courses;

    public CreateModel(CourseService courses)
    {
        _courses = courses;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

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

    public void OnGet()
    {
        // GET renders the empty course-creation form; the model is bound and validated on POST.
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.CreateAsync(
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
        return RedirectToPage("/Courses/Edit", new { id = course!.Id });
    }

    private static string[] SplitTags(string tags)
    {
        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
