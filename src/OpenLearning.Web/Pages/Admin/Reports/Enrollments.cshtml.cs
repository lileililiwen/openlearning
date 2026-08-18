using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Web.Pages.Admin.Reports;

[Authorize(Policy = Policies.RequireAdmin)]
public class EnrollmentsModel : PageModel
{
    private readonly EnrollmentService _enrollments;

    public EnrollmentsModel(EnrollmentService enrollments)
    {
        _enrollments = enrollments;
    }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public List<(DateTime Day, int Count)> OverTime { get; set; } = new();

    public List<EnrollmentService.EnrollmentsByCourseRow> ByCourse { get; set; } = new();

    public int Total => OverTime.Sum(x => x.Count);

    public int MaxDayCount => OverTime.Count == 0 ? 0 : OverTime.Max(x => x.Count);

    private static readonly string[] _csvHeaders = new[] { "Id", "EnrolledAt", "Course", "Student", "Email" };

    public async Task OnGetAsync()
    {
        OverTime = await _enrollments.GetEnrollmentsOverTimeAsync(From, To);
        ByCourse = await _enrollments.GetEnrollmentsByCourseAsync(From, To);
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var enrollments = await _enrollments.GetEnrollmentsForExportAsync(From, To);
        var rows = enrollments.Select(e => new string?[]
        {
            e.Id.ToString(CultureInfo.InvariantCulture),
            e.EnrolledAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            e.Course?.Title ?? string.Empty,
            e.Student?.DisplayName ?? string.Empty,
            e.Student?.Email ?? string.Empty,
        });
        var csv = CsvHelper.Build(
            _csvHeaders,
            rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "enrollments.csv");
    }
}
