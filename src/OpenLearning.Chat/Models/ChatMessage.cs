using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Chat.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string Body { get; set; } = string.Empty;

    /// <summary>Message kind: "chat" (course chat) or "danmu" (video bullet comments).</summary>
    public string Type { get; set; } = "chat";

    /// <summary>Lesson the message belongs to (danmu only; null for course chat).</summary>
    public int? LessonId { get; set; }

    /// <summary>Live session the message belongs to (live chat only; null for course-wide chat).</summary>
    public int? SessionId { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
