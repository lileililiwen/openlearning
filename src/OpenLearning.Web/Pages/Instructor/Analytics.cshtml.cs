using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Analytics.Services;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Web.Pages.Admin.Reports;

namespace OpenLearning.Web.Pages.Instructor;

[Authorize(Policy = Policies.RequireInstructor)]
public class AnalyticsModel : PageModel
{
    private readonly AnalyticsReportService _analytics;

    public AnalyticsModel(AnalyticsReportService analytics)
    {
        _analytics = analytics;
    }

    [BindProperty(SupportsGet = true)]
    public int? CourseId { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public List<Course> Courses { get; set; } = new();

    public EngagementReport? Engagement { get; set; }

    public WorkloadReport? Workload { get; set; }

    public List<AssessmentReport> Assessments { get; set; } = new();

    public FreshnessInfo Freshness { get; set; } = new(null, false);

    public string? Error { get; set; }

    public DateOnly FromDate => DateOnly.FromDateTime(From ?? DateTime.UtcNow.AddDays(-30));

    public DateOnly ToDate => DateOnly.FromDateTime(To ?? DateTime.UtcNow);

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Courses = await _analytics.GetOwnedCoursesAsync(userId);
        Freshness = await _analytics.GetFreshnessAsync();

        if (CourseId.HasValue)
        {
            var (engagement, engError) = await _analytics.GetInstructorEngagementAsync(userId, CourseId.Value, FromDate, ToDate);
            var (workload, wlError) = await _analytics.GetInstructorWorkloadAsync(userId, CourseId.Value, FromDate, ToDate);
            var (assessments, asError) = await _analytics.GetInstructorAssessmentsAsync(userId, CourseId.Value, FromDate, ToDate);
            Error = engError ?? wlError ?? asError;
            if (Error is null)
            {
                Engagement = engagement;
                Workload = workload;
                Assessments = assessments;
            }
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!CourseId.HasValue)
        {
            return RedirectToPage();
        }

        var (workload, error) = await _analytics.GetInstructorWorkloadAsync(userId, CourseId.Value, FromDate, ToDate);
        if (error is not null)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { courseId = CourseId });
        }

        var filters = new { courseId = CourseId, from = From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), to = To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
        await _analytics.AuditExportAsync(userId, "instructor", filters);

        var rows = new List<string?[]>();
        if (workload is not null)
        {
            rows.Add(new string?[]
            {
                workload.CourseId.ToString(CultureInfo.InvariantCulture),
                workload.CourseTitle,
                workload.TeachingHours.ToString("0.0", CultureInfo.InvariantCulture),
                workload.GradingWorkload.ToString(CultureInfo.InvariantCulture),
            });
        }

        var csv = CsvHelper.Build(
            _csvHeaders,
            rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "instructor-analytics.csv");
    }

    private static readonly string[] _csvHeaders = new[] { "CourseId", "Course", "TeachingHours", "GradingWorkload" };
}
