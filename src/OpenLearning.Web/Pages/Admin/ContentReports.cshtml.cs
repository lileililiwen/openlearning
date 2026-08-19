using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Logging.Services;
using OpenLearning.Moderation.Models;
using OpenLearning.Moderation.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "内容举报")]
public class ContentReportsModel : PageModel
{
    private readonly ContentReviewService _contentReview;
    private readonly UserService _users;
    private readonly LogService _logs;

    public ContentReportsModel(ContentReviewService contentReview, UserService users, LogService logs)
    {
        _contentReview = contentReview;
        _users = users;
        _logs = logs;
    }

    public List<PendingReportItem> Pending { get; set; } = new();

    public async Task OnGetAsync()
    {
        var reports = await _contentReview.GetPendingAsync();
        var reporterIds = reports.Select(r => r.ReportedById).Distinct().ToList();
        var users = await _users.GetByIdsAsync(reporterIds);
        var names = users
            .Where(u => u is not null)
            .ToDictionary(u => u!.Id, u => u!.DisplayName);

        var items = new List<PendingReportItem>(reports.Count);
        foreach (var report in reports)
        {
            items.Add(new PendingReportItem(
                report,
                await _contentReview.GetPreviewAsync(report.ContentType, report.ContentId),
                names.TryGetValue(report.ReportedById, out var name) ? name : report.ReportedById));
        }

        Pending = items;
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id)
    {
        return await ResolveAsync(id, true);
    }

    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        return await ResolveAsync(id, false);
    }

    private async Task<IActionResult> ResolveAsync(int id, bool remove)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _contentReview.ResolveAsync(id, remove, adminId);
        if (ok)
        {
            await _logs.RecordAsync(
                adminId,
                User.Identity?.Name ?? string.Empty,
                remove ? "ResolveReportRemove" : "ResolveReportDismiss",
                "ContentReport",
                id.ToString(CultureInfo.InvariantCulture),
                remove ? "Remove content" : "Dismiss",
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Message"] = ResolveMessage(ok, remove, error);
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    private static string ResolveMessage(bool ok, bool remove, string? error)
    {
        if (!ok)
        {
            return error ?? string.Empty;
        }

        return remove ? "Content hidden and report resolved." : "Report dismissed.";
    }

    public static string TypeLabel(ReportedContentType type)
    {
        return type switch
        {
            ReportedContentType.Review => "Review",
            ReportedContentType.ReviewComment => "Review comment",
            ReportedContentType.Question => "Question",
            ReportedContentType.QuestionReply => "Question reply",
            ReportedContentType.Post => "Post",
            ReportedContentType.PostReply => "Post reply",
            _ => type.ToString(),
        };
    }

    public sealed record PendingReportItem(ContentReport Report, ReportPreview? Preview, string ReporterName);
}
