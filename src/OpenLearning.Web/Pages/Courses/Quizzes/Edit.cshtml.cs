using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly QuizService _quizzes;

    public EditModel(QuizService quizzes)
    {
        _quizzes = quizzes;
    }

    public Quiz? Quiz { get; set; }

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
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var quiz = await _quizzes.GetByIdAsync(id);
        if (quiz is null)
        {
            return NotFound();
        }

        if (quiz.Course is null || quiz.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Quiz = quiz;
        Id = id;
        Input.Title = quiz.Title;
        Input.Description = quiz.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            Quiz = await _quizzes.GetByIdAsync(Id);
            return Page();
        }

        var updated = await _quizzes.UpdateAsync(Id, userId, Input.Title, Input.Description);
        if (!updated)
        {
            return Forbid();
        }

        return RedirectToPage(new { id = Id });
    }
}
