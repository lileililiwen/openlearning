using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth.Models;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

public class TakeModel : PageModel
{
    private readonly AttemptService _attempts;
    private readonly EnrollmentService _enrollments;
    private readonly UserManager<ApplicationUser> _userManager;

    public TakeModel(
        AttemptService attempts,
        EnrollmentService enrollments,
        UserManager<ApplicationUser> userManager)
    {
        _attempts = attempts;
        _enrollments = enrollments;
        _userManager = userManager;
    }

    public Quiz? Quiz { get; set; }

    public List<QuizAttempt> Attempts { get; set; } = new();

    [BindProperty]
    public int QuizId { get; set; }

    [BindProperty]
    public Dictionary<int, int> Answers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user?.IsSuspended == true)
        {
            return Forbid();
        }

        var quiz = await _attempts.GetQuizForTakeAsync(id);
        if (quiz is null)
        {
            return NotFound();
        }

        if (!await _enrollments.IsEnrolledAsync(userId, quiz.CourseId))
        {
            return Forbid();
        }

        Quiz = quiz;
        QuizId = id;
        Attempts = await _attempts.GetAttemptsForStudentAsync(userId, id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var user = await _userManager.GetUserAsync(User);
        if (user?.IsSuspended == true)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            Quiz = await _attempts.GetQuizForTakeAsync(QuizId);
            return Page();
        }

        var (attemptId, error) = await _attempts.SubmitAsync(userId, QuizId, Answers);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            Quiz = await _attempts.GetQuizForTakeAsync(QuizId);
            Attempts = await _attempts.GetAttemptsForStudentAsync(userId, QuizId);
            return Page();
        }

        return RedirectToPage("/Courses/Quizzes/Result", new { id = attemptId });
    }
}
