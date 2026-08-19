using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.GradeExport.Models;
using OpenLearning.GradeExport.Services;

namespace OpenLearning.Web.Pages.GradeExport;

/// <summary>One row of the export-jobs history list.</summary>
public sealed record ExportJobRow(
    int Id,
    GradeExportKind Kind,
    AsyncIOJobStatus Status,
    int RowCount,
    DateTime CreatedAt,
    DateTime? FinishedAt,
    string? DownloadKey,
    string? ErrorMessage);

[Authorize]
public class JobsModel : PageModel
{
    private readonly AsyncIOService _asyncIO;
    private readonly DbContext _db;

    public JobsModel(AsyncIOService asyncIO, DbContext db)
    {
        _asyncIO = asyncIO;
        _db = db;
    }

    public List<ExportJobRow> Jobs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(OpenLearning.Auth.Roles.Admin);
        var jobs = await _asyncIO.ListJobsAsync(
            userId, isAdmin, kind: GradeExportService.ExportKind, page: Math.Max(1, PageNumber), pageSize: 20);
        var ids = jobs.Select(j => j.Id).ToList();
        var kinds = new Dictionary<int, GradeExportKind>();
        if (ids.Count > 0)
        {
            var records = await _db.Set<GradeExportJob>().AsNoTracking()
                .Where(g => g.AsyncIOJobId != null && ids.Contains(g.AsyncIOJobId.Value))
                .Select(g => new { g.AsyncIOJobId, g.Kind })
                .ToListAsync();
            foreach (var record in records)
            {
                if (record.AsyncIOJobId is int jobId)
                {
                    kinds[jobId] = record.Kind;
                }
            }
        }

        Jobs = jobs
            .Select(j => new ExportJobRow(
                j.Id,
                kinds.GetValueOrDefault(j.Id, GradeExportKind.CourseRoster),
                j.Status,
                j.TotalRows,
                j.CreatedAt,
                j.FinishedAt,
                j.ResultFileKey,
                j.ErrorMessage))
            .ToList();
    }

    public static string KindLabel(GradeExportKind kind)
    {
        return kind switch
        {
            GradeExportKind.Submissions => "作业提交",
            GradeExportKind.QuizAttempts => "测验成绩",
            GradeExportKind.ExamAttempts => "考试成绩",
            _ => "花名册",
        };
    }

    public static string StatusLabel(AsyncIOJobStatus status)
    {
        return status switch
        {
            AsyncIOJobStatus.Pending => "等待中",
            AsyncIOJobStatus.Running => "生成中",
            AsyncIOJobStatus.Success => "已完成",
            _ => "失败",
        };
    }
}
