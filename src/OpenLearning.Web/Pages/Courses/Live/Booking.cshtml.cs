using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages.Courses.Live;

public class BookingModel : PageModel
{
    private readonly LiveService _live;
    private readonly LiveBookingService _booking;

    public BookingModel(LiveService live, LiveBookingService booking)
    {
        _live = live;
        _booking = booking;
    }

    public LiveSession? Session { get; set; }

    [BindProperty]
    public BookingInputModel Input { get; set; } = new();

    public class BookingInputModel
    {
        public bool IsBookingEnabled { get; set; }

        [Display(Name = "Booking Opens At")]
        public DateTime? BookingOpensAt { get; set; }

        [Display(Name = "Booking Closes At")]
        public DateTime? BookingClosesAt { get; set; }

        [Range(0, int.MaxValue)]
        public int Capacity { get; set; }

        [Display(Name = "Cancellation Deadline")]
        public DateTime? CancellationDeadline { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _live.IsOwnerAsync(id, userId))

        {

            return Forbid();

        }

        var session = await _live.GetByIdAsync(id);
        if (session is null)
        {
            return NotFound();
        }

        Session = session;
        Input = new BookingInputModel
        {
            IsBookingEnabled = session.IsBookingEnabled,
            BookingOpensAt = session.BookingOpensAt?.ToLocalTime(),
            BookingClosesAt = session.BookingClosesAt?.ToLocalTime(),
            Capacity = session.Capacity,
            CancellationDeadline = session.CancellationDeadline?.ToLocalTime(),
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        if (!await _live.IsOwnerAsync(id, userId))

        {

            return Forbid();

        }

        if (!ModelState.IsValid)
        {
            Session = await _live.GetByIdAsync(id);
            return Page();
        }

        var input = new BookingInput(
            Input.IsBookingEnabled,
            Input.BookingOpensAt.HasValue ? DateTime.SpecifyKind(Input.BookingOpensAt.Value, DateTimeKind.Utc) : null,
            Input.BookingClosesAt.HasValue ? DateTime.SpecifyKind(Input.BookingClosesAt.Value, DateTimeKind.Utc) : null,
            Input.Capacity,
            Input.CancellationDeadline.HasValue ? DateTime.SpecifyKind(Input.CancellationDeadline.Value, DateTimeKind.Utc) : null);

        var (ok, error) = await _booking.UpdateBookingConfigAsync(id, userId, input);
        TempData["Message"] = ok ? "Booking configuration updated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";

        var session = await _live.GetByIdAsync(id);
        return RedirectToPage("Index", new { id = session?.CourseId ?? 0 });
    }
}
