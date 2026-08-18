using OpenLearning.Auth.Models;

namespace OpenLearning.CourseManagement.Models;

public enum CourseStatus
{
    Draft = 0,
    Published = 1,
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

    public ICollection<Module> Modules { get; set; } = new List<Module>();

    public bool IsPublished => Status == CourseStatus.Published;

    public bool IsFree => Price is null or <= 0;
}
