using Microsoft.EntityFrameworkCore;
using OpenLearning.Ecommerce.Models;

namespace OpenLearning.Ecommerce.Services;

public class CouponService
{
    private readonly DbContext _db;

    public CouponService(DbContext db)
    {
        _db = db;
    }

    public Task<List<Coupon>> GetAllAsync()
    {
        return _db.Set<Coupon>().AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Validates a coupon code for a user: exists, active, not expired, under
    /// the usage limit, and not already redeemed by the user.
    /// </summary>
    public async Task<(Coupon? Coupon, string? Error)> ValidateAsync(string code, string userId)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await _db.Set<Coupon>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalized);
        if (coupon is null)
        {
            return (null, "Coupon not found.");
        }

        if (!coupon.IsActive)
        {
            return (null, "This coupon is no longer active.");
        }

        if (coupon.ExpiresAt is not null && coupon.ExpiresAt < DateTime.UtcNow)
        {
            return (null, "This coupon has expired.");
        }

        if (coupon.MaxUses is not null && coupon.Uses >= coupon.MaxUses)
        {
            return (null, "This coupon has reached its usage limit.");
        }

        var alreadyUsed = await _db.Set<CouponRedemption>()
            .AnyAsync(r => r.CouponId == coupon.Id && r.UserId == userId);
        if (alreadyUsed)
        {
            return (null, "You have already used this coupon.");
        }

        return (coupon, null);
    }

    /// <summary>Increments the usage count and records one redemption per user.</summary>
    public async Task<(bool Ok, string? Error)> RedeemAsync(int couponId, string userId, int orderId)
    {
        var coupon = await _db.Set<Coupon>().FirstOrDefaultAsync(c => c.Id == couponId);
        if (coupon is null)
        {
            return (false, "Coupon not found.");
        }

        if (coupon.MaxUses is not null && coupon.Uses >= coupon.MaxUses)
        {
            return (false, "Coupon usage limit reached.");
        }

        var alreadyUsed = await _db.Set<CouponRedemption>()
            .AnyAsync(r => r.CouponId == couponId && r.UserId == userId);
        if (alreadyUsed)
        {
            return (false, "You have already used this coupon.");
        }

        coupon.Uses++;
        _db.Set<CouponRedemption>().Add(new CouponRedemption
        {
            CouponId = couponId,
            UserId = userId,
            OrderId = orderId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public static decimal ComputeDiscount(Coupon coupon, decimal price)
    {
        if (coupon.DiscountPercent is > 0)
        {
            return Math.Round(price * coupon.DiscountPercent.Value / 100m, 2);
        }

        if (coupon.DiscountAmount is > 0)
        {
            return Math.Min(coupon.DiscountAmount.Value, price);
        }

        return 0m;
    }

    public async Task<(bool Ok, string? Error)> CreateAsync(
        string code, int? discountPercent, decimal? discountAmount, DateTime? expiresAt, int? maxUses)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length is 0 or > 50)
        {
            return (false, "Code must be 1-50 characters.");
        }

        var hasPercent = discountPercent is > 0 and <= 100;
        var hasAmount = discountAmount is > 0;
        if (hasPercent == hasAmount)
        {
            return (false, "Set exactly one of percent (1-100) or amount (> 0).");
        }

        var exists = await _db.Set<Coupon>().AnyAsync(c => c.Code == normalized);
        if (exists)
        {
            return (false, "A coupon with that code already exists.");
        }

        _db.Set<Coupon>().Add(new Coupon
        {
            Code = normalized,
            DiscountPercent = hasPercent ? discountPercent : null,
            DiscountAmount = hasAmount ? discountAmount : null,
            ExpiresAt = expiresAt,
            MaxUses = maxUses,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
