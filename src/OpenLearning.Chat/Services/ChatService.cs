using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Chat.Services;

public class ChatService
{
    public const int MaxMessageLength = 2000;

    public const string ChatType = "chat";

    public const string DanmuType = "danmu";

    private readonly DbContext _db;

    public ChatService(DbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(int courseId, int limit = 50)
    {
        var messages = await _db.Set<ChatMessage>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .Include(m => m.User)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

        messages.Reverse(); // oldest -> newest for display
        return messages;
    }

    public async Task<bool> IsParticipantAsync(string userId, int courseId)
    {
        return await _db.Set<EnrollmentEntity>().AnyAsync(e => e.StudentId == userId && e.CourseId == courseId)
                || await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    public async Task<ChatMessage?> AddMessageAsync(int courseId, string userId, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > MaxMessageLength)
        {
            return null;
        }

        if (!await IsParticipantAsync(userId, courseId))
        {
            return null;
        }

        var message = new ChatMessage
        {
            CourseId = courseId,
            UserId = userId,
            Body = body.Trim(),
            Type = ChatType,
        };

        _db.Set<ChatMessage>().Add(message);
        await _db.SaveChangesAsync();
        await _db.Entry(message).Reference(m => m.User).LoadAsync();
        return message;
    }

    /// <summary>
    /// Stores a danmu (bullet comment) for a lesson and returns it for broadcast.
    /// The lesson must belong to the course and the user must be a participant.
    /// </summary>
    public async Task<ChatMessage?> AddDanmuAsync(int courseId, int lessonId, string userId, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > MaxMessageLength)
        {
            return null;
        }

        if (!await IsParticipantAsync(userId, courseId))
        {
            return null;
        }

        var lessonBelongsToCourse = await _db.Set<Lesson>()
            .AnyAsync(l => l.Id == lessonId && l.Module!.CourseId == courseId);
        if (!lessonBelongsToCourse)
        {
            return null;
        }

        var message = new ChatMessage
        {
            CourseId = courseId,
            LessonId = lessonId,
            UserId = userId,
            Body = body.Trim(),
            Type = DanmuType,
        };

        _db.Set<ChatMessage>().Add(message);
        await _db.SaveChangesAsync();
        await _db.Entry(message).Reference(m => m.User).LoadAsync();
        return message;
    }

    /// <summary>Recent danmu for a lesson, oldest first (for replay on load).</summary>
    public async Task<List<DanmuItem>> GetLessonDanmuAsync(int lessonId, int limit = 100)
    {
        var rows = await _db.Set<ChatMessage>().AsNoTracking()
            .Where(m => m.Type == DanmuType && m.LessonId.HasValue && m.LessonId.Value == lessonId)
            .OrderBy(m => m.SentAt)
            .Take(limit)
            .Select(m => new { m.UserId, m.Body })
            .ToListAsync();
        if (rows.Count == 0)
        {
            return new List<DanmuItem>();
        }

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var names = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync();
        var nameById = names.ToDictionary(n => n.Id, n => n.DisplayName);
        return rows
            .Select(r => new DanmuItem(nameById.GetValueOrDefault(r.UserId) ?? r.UserId, r.Body))
            .ToList();
    }
}

/// <summary>A danmu (bullet comment) with a resolved display name.</summary>
public sealed record DanmuItem(string UserName, string Body);
