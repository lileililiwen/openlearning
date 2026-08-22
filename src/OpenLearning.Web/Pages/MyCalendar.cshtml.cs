using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages;

[Authorize]
public class MyCalendarModel : PageModel
{
    private readonly LiveBookingService _booking;

    public MyCalendarModel(LiveBookingService booking)
    {
        _booking = booking;
    }

    public List<CalendarFeedEntry> Entries { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1).AddTicks(-1);

        Entries = await _booking.GetCalendarEntriesAsync(userId, from, to);
    }
}
