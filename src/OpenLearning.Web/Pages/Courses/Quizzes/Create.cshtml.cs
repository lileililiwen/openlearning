using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Assessments.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly QuizService _quizzes;

    public CreateModel(QuizService quizzes)
    {
        _quizzes = quizzes;
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
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        CourseId = courseId;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsCourseOwnerAsync(courseId, userId))
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

        var quiz = await _quizzes.CreateAsync(CourseId, userId, Input.Title, Input.Description);
        if (quiz is null)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Quizzes/Edit", new { id = quiz.Id });
    }
}
