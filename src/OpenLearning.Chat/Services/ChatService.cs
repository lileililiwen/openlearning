using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Chat.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Chat.Services;

public class ChatService
{
    public const int MaxMessageLength = 2000;

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
        };

        _db.Set<ChatMessage>().Add(message);
        await _db.SaveChangesAsync();
        await _db.Entry(message).Reference(m => m.User).LoadAsync();
        return message;
    }
}
