using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages.Calendar;

public class FeedModel : PageModel
{
    private readonly LiveBookingService _booking;

    public FeedModel(LiveBookingService booking)
    {
        _booking = booking;
    }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(2).AddTicks(-1);

        var entries = await _booking.GetFeedByTokenAsync(token, from, to);
        if (entries.Count == 0)
        {
            return NotFound();
        }

        var ical = LiveBookingService.RenderIcalFeed(entries);
        return Content(ical, "text/calendar; charset=utf-8");
    }
}
