using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Analytics.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Web.Pages.Admin.Reports;

[Authorize(Policy = Policies.RequireAdmin)]
public class LearningAnalyticsModel : PageModel
{
    private readonly AnalyticsReportService _analytics;

    public LearningAnalyticsModel(AnalyticsReportService analytics)
    {
        _analytics = analytics;
    }

    [BindProperty(SupportsGet = true)]
    public int? CourseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CohortId { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public List<Course> Courses { get; set; } = new();

    public List<OpenLearning.Classes.Models.ClassGroup> Cohorts { get; set; } = new();

    public FunnelReport? Funnel { get; set; }

    public EngagementReport? Engagement { get; set; }

    public CohortRetentionReport? Retention { get; set; }

    public List<AssessmentReport> Assessments { get; set; } = new();

    public FreshnessInfo Freshness { get; set; } = new(null, false);

    public DateOnly FromDate => DateOnly.FromDateTime(From ?? DateTime.UtcNow.AddDays(-30));

    public DateOnly ToDate => DateOnly.FromDateTime(To ?? DateTime.UtcNow);

    public async Task OnGetAsync()
    {
        Courses = await _analytics.GetAllCoursesAsync();
        Freshness = await _analytics.GetFreshnessAsync();

        if (CourseId.HasValue)
        {
            Cohorts = await _analytics.GetCohortsAsync(CourseId.Value);
            Funnel = await _analytics.GetFunnelAsync(CourseId.Value, FromDate, ToDate);
            Engagement = await _analytics.GetEngagementAsync(CourseId.Value, FromDate, ToDate);
            Assessments = await _analytics.GetAdminAssessmentsAsync(CourseId.Value, FromDate, ToDate);
            if (CohortId.HasValue)
            {
                Retention = await _analytics.GetCohortRetentionAsync(CourseId.Value, CohortId.Value, FromDate, ToDate);
            }
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var filters = new { courseId = CourseId, cohortId = CohortId, from = From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), to = To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
        await _analytics.AuditExportAsync(userId, "admin", filters);

        var rows = new List<string?[]>();
        if (CourseId.HasValue)
        {
            var funnel = await _analytics.GetFunnelAsync(CourseId.Value, FromDate, ToDate);
            if (funnel is not null)
            {
                rows.Add(new string?[]
                {
                    funnel.CourseId.ToString(CultureInfo.InvariantCulture),
                    funnel.CourseTitle,
                    funnel.Eligible.ToString(CultureInfo.InvariantCulture),
                    funnel.Enrolled.ToString(CultureInfo.InvariantCulture),
                    funnel.Started.ToString(CultureInfo.InvariantCulture),
                    funnel.Completed.ToString(CultureInfo.InvariantCulture),
                    funnel.CompletionRate.ToString("0.00%", CultureInfo.InvariantCulture),
                });
            }
        }

        var csv = CsvHelper.Build(
            _csvHeaders,
            rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "learning-analytics.csv");
    }

    private static readonly string[] _csvHeaders = new[] { "CourseId", "Course", "Eligible", "Enrolled", "Started", "Completed", "CompletionRate" };
}
