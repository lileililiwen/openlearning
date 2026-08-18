using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Ecommerce.Models;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
}

public class Order
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public decimal Amount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public string PaymentReference { get; set; } = string.Empty;
}
