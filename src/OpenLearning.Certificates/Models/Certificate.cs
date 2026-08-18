using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Certificates.Models;

/// <summary>
/// A completion credential for one enrollment. One certificate per
/// enrollment (unique index) — issued automatically at 100% progress.
/// </summary>
public class Certificate
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public EnrollmentEntity? Enrollment { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Short random token (e.g. CRT-XXXXXX) reserved for future verification.</summary>
    public string Code { get; set; } = string.Empty;
}
