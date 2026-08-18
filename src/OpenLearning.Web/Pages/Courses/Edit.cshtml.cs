using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly CourseService _courses;
    private readonly QuizService _quizzes;

    public EditModel(CourseService courses, QuizService quizzes)
    {
        _courses = courses;
        _quizzes = quizzes;
    }

    public Course? Course { get; set; }

    public List<Quiz> Quizzes { get; set; } = new();

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
        Input.Title = course.Title;
        Input.Description = course.Description;
        Input.Category = course.Category;
        Input.Price = course.Price;
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
            }

            return Page();
        }

        var updated = await _courses.UpdateAsync(id, userId, Input.Title, Input.Description, Input.Category, Input.Price);
        if (!updated)
        {
            return Forbid();
        }

        return RedirectToPage(new { id });
    }
}
