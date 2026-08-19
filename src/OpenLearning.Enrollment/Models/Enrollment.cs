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
}
