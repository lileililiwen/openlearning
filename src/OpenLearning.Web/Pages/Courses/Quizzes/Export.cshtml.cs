using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.QuestionIO.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

[Authorize(Policy = Policies.RequireInstructor)]
public class ExportModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly QuizService _quizzes;
    private readonly QuestionExportService _export;
    private readonly SystemConfigService _config;

    public ExportModel(QuizService quizzes, QuestionExportService export, SystemConfigService config)
    {
        _quizzes = quizzes;
        _export = export;
        _config = config;
    }

    public Quiz? Quiz { get; set; }

    public int? JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int QuizId { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionType? QuestionType { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionDifficulty? Difficulty { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? KnowledgeTag { get; set; }

    public async Task<IActionResult> OnGetAsync(int quizId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsOwnerAsync(quizId, userId))
        {
            return Forbid();
        }

        Quiz = await _quizzes.GetByIdAsync(quizId);
        QuizId = quizId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int quizId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _quizzes.IsOwnerAsync(quizId, userId))
        {
            return Forbid();
        }

        Quiz = await _quizzes.GetByIdAsync(quizId);
        QuizId = quizId;

        var filters = new QuestionExportFilters(quizId, QuestionType, Difficulty, KnowledgeTag, IsBank: false, BankTopic: null);
        var count = await _export.CountAsync(filters, userId, isAdmin: false);
        var syncMax = await _config.GetIntAsync("question.export.syncMaxRows", 5000);
        if (count > syncMax)
        {
            var (jobId, error) = await _export.SubmitExportAsync(filters, userId, isAdmin: false);
            if (error is not null)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            else
            {
                JobId = jobId;
            }

            return Page();
        }

        var (bytes, errorMessage, _) = await _export.ExportSyncAsync(filters, userId, isAdmin: false);
        if (errorMessage is not null || bytes is null)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "导出失败。");
            return Page();
        }

        return File(bytes, _contentTypeXlsx, $"questions-{quizId}.xlsx");
    }
}
