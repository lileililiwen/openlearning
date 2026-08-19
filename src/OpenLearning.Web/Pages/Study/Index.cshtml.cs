using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;

namespace OpenLearning.Web.Pages.Study;

/// <summary>One cell of the month calendar.</summary>
public sealed record CalendarDay(DateOnly Date, bool CheckedIn, int Seconds);

[Authorize]
public class IndexModel : PageModel
{
    private readonly StudyToolService _studyTools;

    public IndexModel(StudyToolService studyTools)
    {
        _studyTools = studyTools;
    }

    public StudyReport Report { get; set; } = new(0, 0, 0, 0);

    public StudyCheckIn? TodayCheckIn { get; set; }

    public List<CalendarDay> Calendar { get; set; } = new();

    public int LeadingBlanks { get; set; }

    public DateOnly Today { get; set; }

    public int Month { get; set; }

    [BindProperty]
    public string? CheckInNote { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Report = await _studyTools.GetReportAsync(userId);

        Today = DateOnly.FromDateTime(DateTime.UtcNow);
        Month = Today.Month;
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(Today.Year, Today.Month);
        var monthEnd = monthStart.AddDays(daysInMonth - 1);

        TodayCheckIn = await _studyTools.GetCheckInAsync(userId, Today);
        var checkIns = await _studyTools.GetCheckInsAsync(userId, monthStart, monthEnd);
        var durations = await _studyTools.GetDailyDurationsAsync(userId, monthStart, monthEnd);

        var checkedInDays = checkIns.Select(c => c.Day).ToHashSet();
        LeadingBlanks = (int)monthStart.DayOfWeek; // Sunday-first grid
        Calendar = Enumerable.Range(1, daysInMonth)
            .Select(day => new CalendarDay(
                new DateOnly(Today.Year, Today.Month, day),
                checkedInDays.Contains(new DateOnly(Today.Year, Today.Month, day)),
                durations.GetValueOrDefault(new DateOnly(Today.Year, Today.Month, day))))
            .ToList();
    }

    public async Task<IActionResult> OnPostCheckInAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _studyTools.CheckInAsync(userId, CheckInNote);
        return RedirectToPage();
    }

    public static string FormatDuration(int seconds)
    {
        var totalMinutes = (int)Math.Ceiling(seconds / 60.0);
        if (totalMinutes < 60)
        {
            return $"{totalMinutes} min";
        }

        return $"{(totalMinutes / 60)} h {totalMinutes % 60} min";
    }

    public static string MonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            _ => "December",
        };
    }
}
