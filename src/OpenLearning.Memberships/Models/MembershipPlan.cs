namespace OpenLearning.Memberships.Models;

/// <summary>A purchasable membership plan with a validity duration.</summary>
public class MembershipPlan
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>A student's membership: the plan, start, and expiry dates.</summary>
public class Membership
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int PlanId { get; set; }

    public MembershipPlan? Plan { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
