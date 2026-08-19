using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Auth;
using OpenLearning.QuestionIO.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.Admin.QuestionBank;

[Authorize(Policy = Policies.RequireAdmin)]
public class ExportModel : PageModel
{
    private const string _contentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly QuestionExportService _export;
    private readonly SystemConfigService _config;

    public ExportModel(QuestionExportService export, SystemConfigService config)
    {
        _export = export;
        _config = config;
    }

    public int? JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionType? QuestionType { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionDifficulty? Difficulty { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? KnowledgeTag { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BankTopic { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var filters = new QuestionExportFilters(QuizId: null, QuestionType, Difficulty, KnowledgeTag, IsBank: true, BankTopic);
        var count = await _export.CountAsync(filters, userId, isAdmin: true);
        var syncMax = await _config.GetIntAsync("question.export.syncMaxRows", 5000);
        if (count > syncMax)
        {
            var (jobId, error) = await _export.SubmitExportAsync(filters, userId, isAdmin: true);
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

        var (bytes, errorMessage, _) = await _export.ExportSyncAsync(filters, userId, isAdmin: true);
        if (errorMessage is not null || bytes is null)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "导出失败。");
            return Page();
        }

        return File(bytes, _contentTypeXlsx, "question-bank-export.xlsx");
    }
}
