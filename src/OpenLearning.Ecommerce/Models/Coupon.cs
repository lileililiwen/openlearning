using OpenLearning.Auth.Models;

namespace OpenLearning.Ecommerce.Models;

/// <summary>An admin-defined discount code. Exactly one of percent/amount applies.</summary>
public class Coupon
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    /// <summary>Percentage discount (e.g. 10 = 10%) when set.</summary>
    public int? DiscountPercent { get; set; }

    /// <summary>Flat-amount discount when set.</summary>
    public decimal? DiscountAmount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>Maximum redemptions; null = unlimited.</summary>
    public int? MaxUses { get; set; }

    public int Uses { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>One use of a coupon by a user (unique per user/coupon).</summary>
public class CouponRedemption
{
    public int Id { get; set; }

    public int CouponId { get; set; }

    public Coupon? Coupon { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int OrderId { get; set; }

    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
}
