using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Live.Models;

namespace OpenLearning.Live.Services;

public sealed record BookingInput(
    bool IsBookingEnabled,
    DateTime? BookingOpensAt,
    DateTime? BookingClosesAt,
    int Capacity,
    DateTime? CancellationDeadline);

public sealed record CalendarFeedEntry(
    string Uid,
    string Summary,
    DateTime Start,
    DateTime End,
    string? Location);

public class LiveBookingService
{
    private readonly DbContext _db;

    public LiveBookingService(DbContext db)
    {
        _db = db;
    }

    public async Task<(bool Ok, string? Error)> UpdateBookingConfigAsync(
        int sessionId, string ownerId, BookingInput input)
    {
        var session = await _db.Set<LiveSession>()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session?.Course is null || session.Course.InstructorId != ownerId)
            return (false, "Session not found or not owned.");

        session.IsBookingEnabled = input.IsBookingEnabled;
        session.BookingOpensAt = input.BookingOpensAt;
        session.BookingClosesAt = input.BookingClosesAt;
        session.Capacity = Math.Max(0, input.Capacity);
        session.CancellationDeadline = input.CancellationDeadline;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, int? Position)> ReserveAsync(int sessionId, string studentId)
    {
        var session = await _db.Set<LiveSession>()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return (false, "Session not found.", null);
        }

        if (!session.IsBookingEnabled)
        {
            return (false, "Booking is not enabled for this session.", null);
        }

        var enrolled = await _db.Set<OpenLearning.Enrollment.Models.Enrollment>()
            .AnyAsync(e => e.CourseId == session.CourseId && e.StudentId == studentId && e.RevokedAt == null);
        if (!enrolled)
        {
            return (false, "You must be enrolled in this course.", null);
        }

        var now = DateTime.UtcNow;
        if (session.BookingOpensAt.HasValue && now < session.BookingOpensAt.Value)
            return (false, $"Booking opens at {session.BookingOpensAt.Value:u}.", null);
        if (session.BookingClosesAt.HasValue && now > session.BookingClosesAt.Value)
            return (false, $"Booking closed at {session.BookingClosesAt.Value:u}.", null);

        var existing = await _db.Set<LiveBooking>()
            .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.StudentId == studentId);
        if (existing is not null)
        {
            if (existing.Status == LiveBookingStatus.Cancelled)
            {
                if (session.Capacity > 0 && await ConfirmedCountAsync(sessionId) >= session.Capacity)
                    return (true, null, await AddToWaitlistAsync(sessionId, studentId));
                existing.Status = LiveBookingStatus.Confirmed;
                existing.BookedAt = now;
                existing.CancelledAt = null;
                await _db.SaveChangesAsync();
                return (true, null, null);
            }
            return (false, "You already have a booking for this session.", null);
        }

        var waitlisted = await _db.Set<LiveWaitlist>()
            .AnyAsync(w => w.SessionId == sessionId && w.StudentId == studentId && w.PromotedAt == null);
        if (waitlisted)
        {
            return (false, "You are already on the waitlist.", null);
        }

        if (session.Capacity > 0 && await ConfirmedCountAsync(sessionId) >= session.Capacity)
            return (true, null, await AddToWaitlistAsync(sessionId, studentId));

        _db.Set<LiveBooking>().Add(new LiveBooking { SessionId = sessionId, StudentId = studentId, Status = LiveBookingStatus.Confirmed });
        await _db.SaveChangesAsync();
        return (true, null, null);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int sessionId, string studentId)
    {
        var session = await _db.Set<LiveSession>().FindAsync(sessionId);
        if (session is null)
        {
            return (false, "Session not found.");
        }

        var booking = await _db.Set<LiveBooking>()
            .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.StudentId == studentId && b.Status == LiveBookingStatus.Confirmed);
        if (booking is null)
        {
            return (false, "No active booking found.");
        }

        if (session.CancellationDeadline.HasValue && DateTime.UtcNow > session.CancellationDeadline.Value)
            return (false, $"Cancellation deadline was {session.CancellationDeadline.Value:u}.");

        booking.Status = LiveBookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(sessionId);
        return (true, null);
    }

    private async Task PromoteFromWaitlistAsync(int sessionId)
    {
        var entry = await _db.Set<LiveWaitlist>()
            .Where(w => w.SessionId == sessionId && w.PromotedAt == null)
            .OrderBy(w => w.Position)
            .FirstOrDefaultAsync();
        if (entry is null)
        {
            return;
        }

        var session = await _db.Set<LiveSession>().FindAsync(sessionId);
        if (session is null)
        {
            return;
        }

        var enrolled = await _db.Set<OpenLearning.Enrollment.Models.Enrollment>()
            .AnyAsync(e => e.CourseId == session.CourseId && e.StudentId == entry.StudentId && e.RevokedAt == null);
        if (!enrolled)
        {
            entry.PromotedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await PromoteFromWaitlistAsync(sessionId);
            return;
        }

        _db.Set<LiveBooking>().Add(new LiveBooking { SessionId = sessionId, StudentId = entry.StudentId, Status = LiveBookingStatus.Confirmed });
        entry.PromotedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<int> AddToWaitlistAsync(int sessionId, string studentId)
    {
        var maxPos = await _db.Set<LiveWaitlist>()
            .Where(w => w.SessionId == sessionId)
            .Select(w => (int?)w.Position).MaxAsync() ?? 0;
        var pos = maxPos + 1;
        _db.Set<LiveWaitlist>().Add(new LiveWaitlist { SessionId = sessionId, StudentId = studentId, Position = pos });
        await _db.SaveChangesAsync();
        return pos;
    }

    private async Task<int> ConfirmedCountAsync(int sessionId)
    {
        return await _db.Set<LiveBooking>()
            .CountAsync(b => b.SessionId == sessionId && b.Status == LiveBookingStatus.Confirmed);
    }

    public Task<LiveBooking?> GetMyBookingAsync(int sessionId, string studentId)
    {
        return _db.Set<LiveBooking>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.StudentId == studentId);
    }

    public async Task<(List<LiveBooking> Bookings, List<LiveWaitlist> Waitlist)> GetRosterAsync(int sessionId)
    {
        var bookings = await _db.Set<LiveBooking>().AsNoTracking()
            .Where(b => b.SessionId == sessionId).OrderBy(b => b.BookedAt).ToListAsync();
        var waitlist = await _db.Set<LiveWaitlist>().AsNoTracking()
            .Where(w => w.SessionId == sessionId).OrderBy(w => w.Position).ToListAsync();
        return (bookings, waitlist);
    }

    public async Task<List<CalendarFeedEntry>> GetCalendarEntriesAsync(string studentId, DateTime from, DateTime to)
    {
        return await _db.Set<LiveBooking>().AsNoTracking()
            .Where(b => b.StudentId == studentId && b.Status == LiveBookingStatus.Confirmed
                && b.Session != null && b.Session.StartsAt >= from && b.Session.StartsAt <= to)
            .Select(b => new CalendarFeedEntry(
                $"live-session-{b.SessionId}", b.Session!.Title, b.Session.StartsAt, b.Session.EndsAt, null))
            .ToListAsync();
    }

    public async Task<(string RawToken, int TokenId)> CreateCalendarTokenAsync(string userId)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = ComputeHash(raw);
        var token = new LiveCalendarToken { UserId = userId, TokenHash = hash };
        _db.Set<LiveCalendarToken>().Add(token);
        await _db.SaveChangesAsync();
        return (raw, token.Id);
    }

    public async Task<bool> RevokeCalendarTokenAsync(int tokenId, string userId)
    {
        var token = await _db.Set<LiveCalendarToken>()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId);
        if (token is null || token.RevokedAt.HasValue)
        {
            return false;
        }
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<LiveCalendarToken>> ListCalendarTokensAsync(string userId)
    {
        return await _db.Set<LiveCalendarToken>().AsNoTracking()
            .Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<CalendarFeedEntry>> GetFeedByTokenAsync(string rawToken, DateTime from, DateTime to)
    {
        var hash = ComputeHash(rawToken);
        var tokenOwner = await _db.Set<LiveCalendarToken>().AsNoTracking()
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .Select(t => (string?)t.UserId).FirstOrDefaultAsync();
        if (tokenOwner is null)
        {
            return new();
        }
        return await GetCalendarEntriesAsync(tokenOwner, from, to);
    }

    public static string RenderIcalFeed(IEnumerable<CalendarFeedEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//OpenLearning//Live Calendar//EN");
        foreach (var e in entries)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.Append("UID:").AppendLine(e.Uid);
            sb.Append("DTSTART:").AppendLine(e.Start.ToUniversalTime().ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
            sb.Append("DTEND:").AppendLine(e.End.ToUniversalTime().ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
            sb.Append("SUMMARY:").AppendLine(EscapeIcal(e.Summary));
            if (e.Location is not null)
                sb.Append("LOCATION:").AppendLine(EscapeIcal(e.Location));
            sb.AppendLine("END:VEVENT");
        }
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static string ComputeHash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string EscapeIcal(string text)
    {
        return text.Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n");
    }
}
