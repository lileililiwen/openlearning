using OpenLearning.Auth.Models;

namespace OpenLearning.UserManagement.Models;

public enum InstructorApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>Self-service instructor application reviewed by an Admin.</summary>
public class InstructorApplication
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Motivation { get; set; } = string.Empty;

    public InstructorApplicationStatus Status { get; set; } = InstructorApplicationStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public string? RejectionReason { get; set; }
}
