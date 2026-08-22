using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Live.Models;
using OpenLearning.Live.Services;

namespace OpenLearning.Web.Pages.Courses.Live;

[Authorize]
public class FeedsModel : PageModel
{
    private readonly LiveBookingService _booking;

    public FeedsModel(LiveBookingService booking)
    {
        _booking = booking;
    }

    public List<LiveCalendarToken> Tokens { get; set; } = new();

    [TempData]
    public string? NewRawToken { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        Tokens = await _booking.ListCalendarTokensAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        var (rawToken, _) = await _booking.CreateCalendarTokenAsync(userId);
        NewRawToken = rawToken;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int tokenId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        await _booking.RevokeCalendarTokenAsync(tokenId, userId);
        return RedirectToPage();
    }
}
