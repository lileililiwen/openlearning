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
    Membership = 6,
    Order = 7,
    Enrollment = 8,
    Assignment = 9,
    Exam = 10,
    Class = 11,
    AsyncIO = 12,
    AssignmentGraded = 13,
    ExamStartingSoon = 14,
    AssignmentDueSoon = 15,
    AssignmentDueMissed = 16,
    ClassStartingSoon = 17,
    EnrollmentExpiringSoon = 18,
    EnrollmentExpired = 19,
    OrderExpired = 20,
    RefundTimeoutRejected = 21,
    InvoiceIssued = 22,
    InvoiceRejected = 23,
    InvoiceVoided = 24,
    InvoiceRedLetterIssued = 25,
    ImportCompleted = 26,
    ImportFailed = 27,
    ExportReady = 28,
    ExportProgress = 29,
    AccountWelcome = 30,
    EnrollmentGrantedBulk = 31,
    IntegrityDisposition = 32,
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

    /// <summary>Optional class scope; notifications with a class id are addressed to that class's members.</summary>
    public int? ClassGroupId { get; set; }

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
