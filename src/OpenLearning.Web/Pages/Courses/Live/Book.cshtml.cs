using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages.Courses.Live;

public class BookModel : PageModel
{
    private readonly LiveService _live;
    private readonly LiveBookingService _booking;

    public BookModel(LiveService live, LiveBookingService booking)
    {
        _live = live;
        _booking = booking;
    }

    public LiveSession? Session { get; set; }
    public LiveBooking? MyBooking { get; set; }
    public int? WaitlistPosition { get; set; }

    public async Task<IActionResult> OnGetAsync(int sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var session = await _live.GetByIdAsync(sessionId);
        if (session is null)
        {
            return NotFound();
        }

        Session = session;
        MyBooking = await _booking.GetMyBookingAsync(sessionId, userId);
        return Page();
    }

    public async Task<IActionResult> OnPostReserveAsync(int sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var session = await _live.GetByIdAsync(sessionId);
        if (session is null)
        {
            return NotFound();
        }

        var (ok, error, position) = await _booking.ReserveAsync(sessionId, userId);
        string msg;
        if (!ok)
        {
            msg = error ?? "Reservation failed.";
        }
        else if (position.HasValue)
        {
            msg = $"Added to waitlist at position {position}.";
        }
        else
        {
            msg = "Seat reserved.";
        }
        TempData["Message"] = msg;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { sessionId });
    }

    public async Task<IActionResult> OnPostCancelAsync(int sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var (ok, error) = await _booking.CancelAsync(sessionId, userId);
        TempData["Message"] = ok ? "Booking cancelled." : error;
        TempData["MessageType"] = ok ? "success" : "danger";

        return RedirectToPage(new { sessionId });
    }
}
