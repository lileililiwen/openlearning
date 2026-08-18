using Microsoft.EntityFrameworkCore;
using OpenLearning.Memberships.Models;

namespace OpenLearning.Memberships.Services;

/// <summary>
/// Membership plans, purchases, renewals, and active-state checks. Renewal
/// extends the existing membership's expiry rather than creating a new row.
/// </summary>
public class MembershipService
{
    private readonly DbContext _db;

    public MembershipService(DbContext db)
    {
        _db = db;
    }

    public Task<List<MembershipPlan>> GetPlansAsync()
    {
        return _db.Set<MembershipPlan>().AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public Task<List<MembershipPlan>> GetAllPlansAsync()
    {
        return _db.Set<MembershipPlan>().AsNoTracking()
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public Task<MembershipPlan?> GetPlanByIdAsync(int planId)
    {
        return _db.Set<MembershipPlan>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<(bool Ok, string? Error)> CreatePlanAsync(
        string name, string description, decimal price, int durationDays)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length is 0 or > 100)
        {
            return (false, "Plan name is required (100 characters or fewer).");
        }

        if (price < 0)
        {
            return (false, "Price cannot be negative.");
        }

        if (durationDays < 1)
        {
            return (false, "Duration must be at least 1 day.");
        }

        _db.Set<MembershipPlan>().Add(new MembershipPlan
        {
            Name = trimmedName,
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            DurationDays = durationDays,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetPlanActiveAsync(int planId, bool isActive)
    {
        var plan = await _db.Set<MembershipPlan>().FindAsync(planId);
        if (plan is null)
        {
            return (false, "Plan not found.");
        }

        plan.IsActive = isActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Purchases a plan, or renews an existing membership by extending expiry.</summary>
    public async Task<(bool Ok, string? Error)> PurchaseAsync(string userId, int planId)
    {
        var plan = await _db.Set<MembershipPlan>()
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
        if (plan is null)
        {
            return (false, "Membership plan not found or inactive.");
        }

        var active = await GetActiveAsync(userId);
        if (active is not null && active.ExpiresAt >= DateTime.UtcNow)
        {
            // Renewal: extend from the current expiry (or now if it lapsed).
            var baseDate = active.ExpiresAt > DateTime.UtcNow ? active.ExpiresAt : DateTime.UtcNow;
            active.ExpiresAt = baseDate.AddDays(plan.DurationDays);
        }
        else
        {
            _db.Set<Membership>().Add(new Membership
            {
                UserId = userId,
                PlanId = plan.Id,
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(plan.DurationDays),
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<Membership?> GetActiveAsync(string userId)
    {
        return _db.Set<Membership>()
            .Include(m => m.Plan)
            .Where(m => m.UserId == userId && m.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(m => m.ExpiresAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsActiveAsync(string userId)
    {
        return await _db.Set<Membership>()
            .AnyAsync(m => m.UserId == userId && m.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>Active memberships expiring within the given window (for reminders).</summary>
    public Task<List<Membership>> GetExpiringAsync(int withinDays)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(withinDays);
        return _db.Set<Membership>()
            .Include(m => m.Plan)
            .Where(m => m.ExpiresAt > now && m.ExpiresAt <= horizon)
            .ToListAsync();
    }

    public async Task<bool> MarkExpiredAsync(int membershipId)
    {
        var membership = await _db.Set<Membership>().FindAsync(membershipId);
        if (membership is null)
        {
            return false;
        }

        membership.ExpiresAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
