using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Ratings.Models;

/// <summary>
/// A single student's rating + optional review for a course. One per
/// (Course, User) — re-submitting replaces the prior row.
/// </summary>
public class Review
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>1..5 stars.</summary>
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hidden by an admin after a content report; excluded from all reads.</summary>
    public bool IsHidden { get; set; }

    public ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
}
