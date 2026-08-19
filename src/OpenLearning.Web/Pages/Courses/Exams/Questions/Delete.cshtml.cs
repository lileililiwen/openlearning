using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Auth;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams.Questions;

[Authorize(Policy = Policies.RequireInstructor)]
public class DeleteModel : PageModel
{
    private readonly ExamService _exams;

    public DeleteModel(ExamService exams)
    {
        _exams = exams;
    }

    public Question? Question { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var question = await _exams.GetQuestionAsync(id);
        if (question is null || question.ExamId is null)
        {
            return NotFound();
        }

        if (!await _exams.IsOwnerAsync(question.ExamId.Value, userId))
        {
            return Forbid();
        }

        Question = question;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var question = await _exams.GetQuestionAsync(id);
        if (question is null || question.ExamId is null)
        {
            return NotFound();
        }

        var deleted = await _exams.DeleteQuestionAsync(id, userId);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToPage("/Courses/Exams/Edit", new { id = question.ExamId.Value });
    }
}
