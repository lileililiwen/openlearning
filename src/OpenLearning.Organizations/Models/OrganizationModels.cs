using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Organizations.Models;

public enum OrganizationStatus { Active, Suspended }
public enum OrganizationRole { OrganizationAdmin, Instructor, Manager, Learner }
public enum MembershipStatus { Active, Suspended }

public sealed class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public OrganizationStatus Status { get; set; }
    public string PrimaryColor { get; set; } = "#0d6efd";
    public int MaximumDepartmentDepth { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Department
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public int? ParentId { get; set; }
    public Department? Parent { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class OrganizationMembership
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public OrganizationRole Role { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public sealed class OrganizationInvitation
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}

public sealed class OrganizationCourse
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
}

public sealed class OrganizationAudit
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
