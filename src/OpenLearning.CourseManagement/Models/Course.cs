using OpenLearning.Auth.Models;

namespace OpenLearning.CourseManagement.Models;

public enum CourseStatus
{
    Draft = 0,
    Published = 1,

    /// <summary>Submitted for review; an admin approves to publish or rejects back to draft.</summary>
    UnderReview = 2,
}

public enum CourseLevel
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
}

public class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    /// <summary>Purchase price; null or zero means the course is free.</summary>
    public decimal? Price { get; set; }

    public string InstructorId { get; set; } = string.Empty;

    public ApplicationUser? Instructor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Admin note attached when a course is rejected during content review.</summary>
    public string? ReviewNote { get; set; }

    /// <summary>Default access period in days for new enrollments; null = unlimited.</summary>
    public int? DefaultAccessDays { get; set; }

    // ===== Course discovery metadata =====

    public CourseLevel? Level { get; set; }

    /// <summary>Free-form duration hint, e.g. "6 hours" or "4 weeks".</summary>
    public string Duration { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Prerequisites { get; set; } = string.Empty;

    public string LearningOutcomes { get; set; } = string.Empty;

    public ICollection<Module> Modules { get; set; } = new List<Module>();

    public ICollection<CourseTag> Tags { get; set; } = new List<CourseTag>();

    public bool IsPublished => Status == CourseStatus.Published;

    public bool IsFree => Price is null or <= 0;
}
