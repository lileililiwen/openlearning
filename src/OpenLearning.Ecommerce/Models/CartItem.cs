using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Ecommerce.Models;

/// <summary>A course a Student has added to their shopping cart (one per student/course).</summary>
public class CartItem
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
