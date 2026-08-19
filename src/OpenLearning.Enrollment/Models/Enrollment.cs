using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Enrollment.Models;

public class Enrollment
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Optional class group (term/cohort) the enrollment belongs to; null = course-wide.</summary>
    public int? ClassGroupId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>When access expires; null = no expiry (unlimited access).</summary>
    public DateTime? AccessExpiresAt { get; set; }

    /// <summary>When access was revoked (expiry job, refund, or admin action); null = active.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Why access was revoked: "expired", "refund", "admin", etc.</summary>
    public string? RevokedReason { get; set; }
}
