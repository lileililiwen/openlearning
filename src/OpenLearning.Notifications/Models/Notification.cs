using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Notifications.Models;

/// <summary>Event categories that drive notification content.</summary>
public enum NotificationType
{
    Course = 0,
    Lesson = 1,
    Quiz = 2,
    Certificate = 3,
    Announcement = 4,
    Application = 5,
}

/// <summary>One in-app notification for a user.</summary>
public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>Relative app URL the notification points to, e.g. "/Courses/Details?id=1".</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>An instructor-authored message shown to everyone enrolled in a course.</summary>
public class CourseAnnouncement
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
