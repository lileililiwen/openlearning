using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Distribution.Models;

public enum CommissionStatus
{
    Pending = 0,
    Available = 1,
    Paid = 2,
    Reversed = 3,
}

public enum PayoutStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>Distribution profile for a user holding the Distributor role.</summary>
public class DistributorProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A distributor's share link for one published course.</summary>
public class AffiliateLink
{
    public int Id { get; set; }

    public string DistributorUserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Unique short slug used in /D/C/{slug}.</summary>
    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One visit through an affiliate link.</summary>
public class AffiliateClick
{
    public int Id { get; set; }

    public int AffiliateLinkId { get; set; }

    public AffiliateLink? AffiliateLink { get; set; }

    /// <summary>Anonymous id from the first-party ol_aff cookie.</summary>
    public string AnonymousId { get; set; } = string.Empty;

    public string? HashedIp { get; set; }

    public string? UserAgent { get; set; }

    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Links a paid order to the affiliate click that preceded it.</summary>
public class Attribution
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int AffiliateClickId { get; set; }

    public string DistributorUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One commission for a distributor, tied to an order.</summary>
public class CommissionEntry
{
    public int Id { get; set; }

    public string DistributorUserId { get; set; } = string.Empty;

    public int OrderId { get; set; }

    /// <summary>Positive = commission; negative = clawback on a post-payout refund.</summary>
    public decimal Amount { get; set; }

    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;

    public int? PayoutRequestId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A distributor's payout request, reviewed by Admin/Finance.</summary>
public class PayoutRequest
{
    public int Id { get; set; }

    public string DistributorUserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public decimal Amount { get; set; }

    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }
}

/// <summary>Frozen distributor earnings for a closed period.</summary>
public class DistributorSettlementStatement
{
    public int Id { get; set; }

    public string DistributorUserId { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
