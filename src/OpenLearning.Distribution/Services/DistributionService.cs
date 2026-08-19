using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Distribution.Models;
using OpenLearning.Ecommerce.Models;

namespace OpenLearning.Distribution.Services;

/// <summary>
/// Affiliate distribution: share links, click attribution, commissions,
/// payouts, and period settlements.
/// </summary>
public class DistributionService
{
    /// <summary>Commission rate applied to an attributed paid order.</summary>
    public const decimal CommissionRate = 0.10m;

    /// <summary>Attribution window for a click before it expires.</summary>
    public static readonly TimeSpan AttributionWindow = TimeSpan.FromDays(30);

    private readonly DbContext _db;

    public DistributionService(DbContext db)
    {
        _db = db;
    }

    // ===== Profiles & links =====

    public async Task EnsureProfileAsync(string userId)
    {
        if (await _db.Set<DistributorProfile>().AnyAsync(p => p.UserId == userId))
        {
            return;
        }

        _db.Set<DistributorProfile>().Add(new DistributorProfile { UserId = userId });
        await _db.SaveChangesAsync();
    }

    public Task<List<DistributorProfile>> GetProfilesAsync()
    {
        return _db.Set<DistributorProfile>().AsNoTracking()
            .Include(p => p.User)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task SetActiveAsync(string userId, bool isActive)
    {
        var profile = await _db.Set<DistributorProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            return;
        }

        profile.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    public async Task<(AffiliateLink? Link, string? Error)> CreateLinkAsync(string distributorId, int courseId)
    {
        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.Status == CourseStatus.Published);
        if (course is null)
        {
            return (null, "Course not found or not published.");
        }

        var existing = await _db.Set<AffiliateLink>()
            .FirstOrDefaultAsync(l => l.DistributorUserId == distributorId && l.CourseId == courseId);
        if (existing is not null)
        {
            return (existing, null);
        }

        var slug = await GenerateUniqueSlugAsync();
        var link = new AffiliateLink
        {
            DistributorUserId = distributorId,
            CourseId = courseId,
            Slug = slug,
        };
        _db.Set<AffiliateLink>().Add(link);
        await _db.SaveChangesAsync();
        return (link, null);
    }

    public Task<List<AffiliateLink>> GetLinksAsync(string distributorId)
    {
        return _db.Set<AffiliateLink>().AsNoTracking()
            .Include(l => l.Course)
            .Where(l => l.DistributorUserId == distributorId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public Task<AffiliateLink?> GetLinkBySlugAsync(string slug)
    {
        return _db.Set<AffiliateLink>().AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Slug == slug);
    }

    public async Task RecordClickAsync(int linkId, string anonymousId, string? hashedIp, string? userAgent)
    {
        _db.Set<AffiliateClick>().Add(new AffiliateClick
        {
            AffiliateLinkId = linkId,
            AnonymousId = anonymousId,
            HashedIp = hashedIp,
            UserAgent = userAgent,
        });
        await _db.SaveChangesAsync();
    }

    // ===== Attribution & commissions =====

    /// <summary>
    /// Attributes a paid order to a distributor when a click for the same
    /// course + anonymous id exists within the attribution window. Idempotent.
    /// </summary>
    public async Task RecordPaidAsync(int orderId, string? anonymousId)
    {
        if (string.IsNullOrWhiteSpace(anonymousId))
        {
            return;
        }

        if (await _db.Set<Attribution>().AnyAsync(a => a.OrderId == orderId))
        {
            return;
        }

        var order = await _db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Paid);
        if (order is null)
        {
            return;
        }

        var cutoff = DateTime.UtcNow - AttributionWindow;
        var click = await _db.Set<AffiliateClick>().AsNoTracking()
            .Include(c => c.AffiliateLink)
            .Where(c => c.AnonymousId == anonymousId
                && c.AffiliateLink!.CourseId == order.CourseId
                && c.ClickedAt >= cutoff)
            .OrderByDescending(c => c.ClickedAt)
            .FirstOrDefaultAsync();
        if (click?.AffiliateLink is null)
        {
            return;
        }

        _db.Set<Attribution>().Add(new Attribution
        {
            OrderId = orderId,
            AffiliateClickId = click.Id,
            DistributorUserId = click.AffiliateLink.DistributorUserId,
        });
        _db.Set<CommissionEntry>().Add(new CommissionEntry
        {
            DistributorUserId = click.AffiliateLink.DistributorUserId,
            OrderId = orderId,
            Amount = Math.Round(order.Amount * CommissionRate, 2),
            Status = CommissionStatus.Pending,
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>Reverses the commission for a refunded order (clawback if already paid).</summary>
    public async Task ReverseForOrderAsync(int orderId)
    {
        var attribution = await _db.Set<Attribution>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.OrderId == orderId);
        if (attribution is null)
        {
            return;
        }

        var entry = await _db.Set<CommissionEntry>()
            .FirstOrDefaultAsync(c => c.OrderId == orderId && c.Amount > 0);
        if (entry is null)
        {
            return;
        }

        if (entry.Status == CommissionStatus.Paid)
        {
            // Clawback for the next payout cycle.
            _db.Set<CommissionEntry>().Add(new CommissionEntry
            {
                DistributorUserId = attribution.DistributorUserId,
                OrderId = orderId,
                Amount = -entry.Amount,
                Status = CommissionStatus.Available,
            });
        }
        else
        {
            entry.Status = CommissionStatus.Reversed;
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>Moves commissions past the holding period to Available. Returns the count.</summary>
    public async Task<int> TransitionHeldAsync(TimeSpan holdingPeriod)
    {
        var cutoff = DateTime.UtcNow - holdingPeriod;
        var held = await _db.Set<CommissionEntry>()
            .Where(c => c.Status == CommissionStatus.Pending && c.CreatedAt < cutoff)
            .ToListAsync();
        foreach (var entry in held)
        {
            entry.Status = CommissionStatus.Available;
        }

        if (held.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return held.Count;
    }

    // ===== Balances =====

    public async Task<decimal> GetTotalEarnedAsync(string distributorId)
    {
        return await _db.Set<CommissionEntry>()
            .Where(c => c.DistributorUserId == distributorId)
            .SumAsync(c => (decimal?)c.Amount) ?? 0m;
    }

    public async Task<decimal> GetAvailableAsync(string distributorId)
    {
        var available = await _db.Set<CommissionEntry>()
            .Where(c => c.DistributorUserId == distributorId
                && (c.Status == CommissionStatus.Available || c.Status == CommissionStatus.Pending))
            .SumAsync(c => (decimal?)c.Amount) ?? 0m;
        var reserved = await _db.Set<PayoutRequest>()
            .Where(p => p.DistributorUserId == distributorId && p.Status == PayoutStatus.Pending)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        return Math.Round(Math.Max(0m, available - reserved), 2);
    }

    public Task<List<CommissionEntry>> GetCommissionsAsync(string distributorId, CommissionStatus? status = null, int page = 1, int pageSize = 50)
    {
        IQueryable<CommissionEntry> query = _db.Set<CommissionEntry>().AsNoTracking()
            .Where(c => c.DistributorUserId == distributorId);
        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        return query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // ===== Payouts =====

    public async Task<(bool Ok, string? Error)> RequestPayoutAsync(string distributorId, decimal amount)
    {
        if (amount <= 0)
        {
            return (false, "Amount must be positive.");
        }

        var available = await GetAvailableAsync(distributorId);
        if (amount > available)
        {
            return (false, "Requested amount exceeds your available balance.");
        }

        _db.Set<PayoutRequest>().Add(new PayoutRequest
        {
            DistributorUserId = distributorId,
            Amount = amount,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<List<PayoutRequest>> GetPayoutsAsync(string distributorId)
    {
        return _db.Set<PayoutRequest>().AsNoTracking()
            .Where(p => p.DistributorUserId == distributorId)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync();
    }

    public Task<List<PayoutRequest>> ListPendingPayoutsAsync()
    {
        return _db.Set<PayoutRequest>().AsNoTracking()
            .Where(p => p.Status == PayoutStatus.Pending)
            .Include(p => p.User)
            .OrderBy(p => p.RequestedAt)
            .ToListAsync();
    }

    public async Task<(bool Ok, string? Error)> ApprovePayoutAsync(int payoutId)
    {
        var payout = await _db.Set<PayoutRequest>().FirstOrDefaultAsync(p => p.Id == payoutId);
        if (payout is null)
        {
            return (false, "Payout not found.");
        }

        if (payout.Status != PayoutStatus.Pending)
        {
            return (false, "This payout was already reviewed.");
        }

        payout.Status = PayoutStatus.Approved;
        payout.ReviewedAt = DateTime.UtcNow;

        // Mark Available commissions as Paid until the payout amount is covered.
        var entries = await _db.Set<CommissionEntry>()
            .Where(c => c.DistributorUserId == payout.DistributorUserId
                && c.Status == CommissionStatus.Available && c.Amount > 0)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        var remaining = payout.Amount;
        foreach (var entry in entries)
        {
            if (remaining <= 0)
            {
                break;
            }

            entry.Status = CommissionStatus.Paid;
            entry.PayoutRequestId = payout.Id;
            remaining -= entry.Amount;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RejectPayoutAsync(int payoutId, string? note)
    {
        var payout = await _db.Set<PayoutRequest>().FirstOrDefaultAsync(p => p.Id == payoutId);
        if (payout is null)
        {
            return (false, "Payout not found.");
        }

        if (payout.Status != PayoutStatus.Pending)
        {
            return (false, "This payout was already reviewed.");
        }

        payout.Status = PayoutStatus.Rejected;
        payout.ReviewedAt = DateTime.UtcNow;
        payout.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Settlement =====

    /// <summary>Freezes each distributor's period earnings into an immutable statement. Idempotent.</summary>
    public async Task<int> ClosePeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        var alreadyClosed = (await _db.Set<DistributorSettlementStatement>()
                .Where(s => s.PeriodStart == periodStart)
                .Select(s => s.DistributorUserId)
                .ToListAsync())
            .ToHashSet();

        var rows = await _db.Set<CommissionEntry>().AsNoTracking()
            .Where(c => c.CreatedAt >= periodStart && c.CreatedAt < periodEnd)
            .Select(c => new { c.DistributorUserId, c.Amount, c.Status })
            .ToListAsync();

        var totals = rows
            .Where(r => r.Status == CommissionStatus.Available || r.Status == CommissionStatus.Paid)
            .GroupBy(r => r.DistributorUserId)
            .Select(g => (DistributorUserId: g.Key, Amount: g.Sum(r => r.Amount)))
            .Where(t => t.Amount != 0m && !alreadyClosed.Contains(t.DistributorUserId))
            .ToList();

        foreach (var total in totals)
        {
            _db.Set<DistributorSettlementStatement>().Add(new DistributorSettlementStatement
            {
                DistributorUserId = total.DistributorUserId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalAmount = total.Amount,
            });
        }

        if (totals.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return totals.Count;
    }

    public Task<List<DistributorSettlementStatement>> GetStatementsAsync(string? distributorId = null)
    {
        IQueryable<DistributorSettlementStatement> query = _db.Set<DistributorSettlementStatement>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(distributorId))
        {
            query = query.Where(s => s.DistributorUserId == distributorId);
        }

        return query.OrderByDescending(s => s.PeriodStart).ToListAsync();
    }

    // ===== Helpers =====

    private async Task<string> GenerateUniqueSlugAsync()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var slug = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(5)).ToLowerInvariant();
            if (!await _db.Set<AffiliateLink>().AnyAsync(l => l.Slug == slug))
            {
                return slug;
            }
        }

        return Guid.NewGuid().ToString("N")[..10];
    }
}
