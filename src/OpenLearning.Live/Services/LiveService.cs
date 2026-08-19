using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Models;
using OpenLearning.Chat.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Live.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Live.Services;

/// <summary>Form input for creating or editing a live session.</summary>
public sealed record LiveInput(string Title, string? Description, DateTime StartsAt, DateTime EndsAt);

/// <summary>
/// Live session management: owner-gated CRUD, status transitions, co-hosting,
/// one-time check-ins, and per-session chat persistence. Students must be
/// enrolled (or own/co-host/admin) to join a session.
/// </summary>
public class LiveService
{
    private const string _defaultStreamUrlTemplate =
#pragma warning disable S1075 // Configurable placeholder for the external streaming provider.
        "https://cdn.example.com/live/{key}/index.m3u8";
#pragma warning restore S1075

    private readonly DbContext _db;
    private readonly IConfiguration _config;

    public LiveService(DbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ===== CRUD =====

    public async Task<(bool Ok, string? Error)> CreateAsync(int courseId, string ownerId, LiveInput input)
    {
        var course = await _db.Set<Course>().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (false, "Course not found.");
        }

        if (course.InstructorId != ownerId)
        {
            return (false, "Only the course instructor can manage live sessions.");
        }

        var (valid, error) = Validate(input);
        if (!valid)
        {
            return (false, error);
        }

        var key = "live-" + Guid.NewGuid().ToString("N");
        _db.Set<LiveSession>().Add(new LiveSession
        {
            CourseId = courseId,
            InstructorId = ownerId,
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            StartsAt = input.StartsAt,
            EndsAt = input.EndsAt,
            StreamKey = key,
            StreamUrl = ResolveStreamUrl(key),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(int sessionId, string ownerId, LiveInput input)
    {
        var session = await _db.Set<LiveSession>().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return (false, "Live session not found.");
        }

        if (session.InstructorId != ownerId)
        {
            return (false, "Only the course instructor can manage live sessions.");
        }

        var (valid, error) = Validate(input);
        if (!valid)
        {
            return (false, error);
        }

        session.Title = input.Title.Trim();
        session.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        session.StartsAt = input.StartsAt;
        session.EndsAt = input.EndsAt;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int sessionId, string ownerId)
    {
        var session = await _db.Set<LiveSession>().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null || session.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<LiveSession>().Remove(session);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== Queries =====

    public Task<List<LiveSession>> GetForCourseAsync(int courseId)
    {
        return _db.Set<LiveSession>().AsNoTracking()
            .Where(s => s.CourseId == courseId)
            .Include(s => s.CoHosts)
            .OrderBy(s => s.StartsAt)
            .ToListAsync();
    }

    public Task<LiveSession?> GetByIdAsync(int id)
    {
        return _db.Set<LiveSession>().AsNoTracking()
            .Include(s => s.Instructor)
            .Include(s => s.CoHosts).ThenInclude(h => h.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> IsOwnerAsync(int sessionId, string userId)
    {
        return await _db.Set<LiveSession>().AnyAsync(s => s.Id == sessionId && s.InstructorId == userId);
    }

    public async Task<bool> IsCoHostAsync(int sessionId, string userId)
    {
        return await _db.Set<LiveCoHost>().AnyAsync(h => h.SessionId == sessionId && h.UserId == userId);
    }

    /// <summary>Who may view/chat/check-in: course owner, co-host, admin, or an enrolled student.</summary>
    public async Task<bool> CanAccessAsync(int sessionId, string userId, bool isAdmin)
    {
        var session = await _db.Set<LiveSession>().AsNoTracking()
            .Select(s => new { s.Id, s.InstructorId, s.CourseId })
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return false;
        }

        if (isAdmin || session.InstructorId == userId || await IsCoHostAsync(sessionId, userId))
        {
            return true;
        }

        return await _db.Set<EnrollmentEntity>()
            .AnyAsync(e => e.StudentId == userId && e.CourseId == session.CourseId);
    }

    /// <summary>Who may start/end a session: course owner or co-host.</summary>
    public async Task<bool> CanManageAsync(int sessionId, string userId)
    {
        return await IsOwnerAsync(sessionId, userId) || await IsCoHostAsync(sessionId, userId);
    }

    // ===== Status =====

    public async Task<(bool Ok, string? Error)> StartAsync(int sessionId, string userId)
    {
        var session = await _db.Set<LiveSession>().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return (false, "Live session not found.");
        }

        if (session.InstructorId != userId && !await IsCoHostAsync(sessionId, userId))
        {
            return (false, "Only the instructor or a co-host can start the session.");
        }

        if (session.Status == LiveSessionStatus.Ended)
        {
            return (false, "This session has already ended.");
        }

        session.Status = LiveSessionStatus.Live;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> EndAsync(int sessionId, string userId, int? recordingFileId)
    {
        var session = await _db.Set<LiveSession>().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return (false, "Live session not found.");
        }

        if (session.InstructorId != userId && !await IsCoHostAsync(sessionId, userId))
        {
            return (false, "Only the instructor or a co-host can end the session.");
        }

        session.Status = LiveSessionStatus.Ended;
        session.RecordingFileId = recordingFileId;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Co-hosts =====

    public async Task<(bool Ok, string? Error)> AddCoHostAsync(int sessionId, string ownerId, string email)
    {
        var session = await _db.Set<LiveSession>().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return (false, "Live session not found.");
        }

        if (session.InstructorId != ownerId)
        {
            return (false, "Only the course instructor can manage co-hosts.");
        }

        var trimmed = (email ?? string.Empty).Trim();
        var user = await _db.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Email != null && u.Email == trimmed);
        if (user is null)
        {
            return (false, "No user has that email address.");
        }

        if (user.Id == session.InstructorId)
        {
            return (false, "The instructor is already a host.");
        }

        if (await _db.Set<LiveCoHost>().AnyAsync(h => h.SessionId == sessionId && h.UserId == user.Id))
        {
            return (false, "That user is already a co-host.");
        }

        _db.Set<LiveCoHost>().Add(new LiveCoHost { SessionId = sessionId, UserId = user.Id });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveCoHostAsync(int sessionId, string ownerId, string userId)
    {
        var session = await _db.Set<LiveSession>().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null || session.InstructorId != ownerId)
        {
            return false;
        }

        var host = await _db.Set<LiveCoHost>()
            .FirstOrDefaultAsync(h => h.SessionId == sessionId && h.UserId == userId);
        if (host is null)
        {
            return false;
        }

        _db.Set<LiveCoHost>().Remove(host);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== Check-in =====

    public async Task<(bool Ok, string? Error)> CheckInAsync(int sessionId, string userId)
    {
        var session = await _db.Set<LiveSession>().AsNoTracking()
            .Select(s => new { s.Id, s.Status, s.StartsAt, s.EndsAt, s.CourseId })
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return (false, "Live session not found.");
        }

        if (session.Status != LiveSessionStatus.Live)
        {
            return (false, "Check-in is only available while the session is live.");
        }

        var now = DateTime.UtcNow;
        if (now < session.StartsAt || now > session.EndsAt)
        {
            return (false, "Check-in is only available during the session window.");
        }

        if (await _db.Set<LiveCheckIn>().AnyAsync(c => c.SessionId == sessionId && c.UserId == userId))
        {
            return (false, "You already checked in.");
        }

        _db.Set<LiveCheckIn>().Add(new LiveCheckIn { SessionId = sessionId, UserId = userId });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<bool> HasCheckedInAsync(int sessionId, string userId)
    {
        return _db.Set<LiveCheckIn>().AnyAsync(c => c.SessionId == sessionId && c.UserId == userId);
    }

    // ===== Live chat =====

    public async Task<ChatMessage?> AddLiveMessageAsync(int sessionId, string userId, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > ChatService.MaxMessageLength)
        {
            return null;
        }

        if (!await CanAccessAsync(sessionId, userId, isAdmin: false))
        {
            return null;
        }

        var session = await _db.Set<LiveSession>().AsNoTracking()
            .Select(s => new { s.Id, s.CourseId })
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var message = new ChatMessage
        {
            CourseId = session.CourseId,
            SessionId = sessionId,
            UserId = userId,
            Body = body.Trim(),
            Type = ChatService.ChatType,
        };

        _db.Set<ChatMessage>().Add(message);
        await _db.SaveChangesAsync();
        await _db.Entry(message).Reference(m => m.User).LoadAsync();
        return message;
    }

    public async Task<List<ChatMessage>> GetLiveMessagesAsync(int sessionId, int limit = 50)
    {
        var messages = await _db.Set<ChatMessage>().AsNoTracking()
            .Where(m => m.SessionId.HasValue && m.SessionId.Value == sessionId && m.Type == ChatService.ChatType)
            .Include(m => m.User)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

        messages.Reverse(); // oldest -> newest for display
        return messages;
    }

    // ===== Helpers =====

    private static (bool Valid, string? Error) Validate(LiveInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return (false, "Title is required.");
        }

        if (input.Title.Trim().Length > 200)
        {
            return (false, "Title must be 200 characters or fewer.");
        }

        if (input.EndsAt <= input.StartsAt)
        {
            return (false, "End time must be after the start time.");
        }

        return (true, null);
    }

    private string ResolveStreamUrl(string key)
    {
        // The pull URL template can be configured via "Live:StreamUrlTemplate"
        // with a {key} placeholder; without configuration a placeholder domain
        // keeps the player wired up for testing.
        var template = _config["Live:StreamUrlTemplate"];
        if (string.IsNullOrWhiteSpace(template))
        {
            template = _defaultStreamUrlTemplate;
        }

        return template.Replace("{key}", key);
    }
}
