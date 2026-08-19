using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Settlement.Models;

namespace OpenLearning.Settlement.Services;

/// <summary>
/// Instructor settlement: ledger credits on paid orders, refund reversals,
/// and withdrawal requests. The available balance is the ledger sum minus
/// amounts reserved by pending/paid withdrawals.
/// </summary>
public class SettlementService
{
    /// <summary>Minimum available balance required to request a withdrawal.</summary>
    public const decimal MinWithdrawalAmount = 10m;

    private readonly DbContext _db;

    public SettlementService(DbContext db)
    {
        _db = db;
    }

    /// <summary>Posts a ledger entry. Amount may be negative for refund reversals.</summary>
    public async Task CreditAsync(string instructorId, int? courseId, decimal amount, string reason)
    {
        _db.Set<SettlementLedger>().Add(new SettlementLedger
        {
            InstructorId = instructorId,
            CourseId = courseId,
            Amount = amount,
            Reason = reason,
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Freezes each instructor's net earnings for a period into a
    /// <see cref="SettlementStatement"/>. Idempotent: instructors that already
    /// have a statement for <paramref name="periodStart"/> are skipped.
    /// Returns the number of statements created.
    /// </summary>
    public async Task<int> CloseInstructorPeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        var alreadyClosed = (await _db.Set<SettlementStatement>()
                .Where(s => s.PeriodStart == periodStart)
                .Select(s => s.InstructorId)
                .ToListAsync())
            .ToHashSet();

        var rows = await _db.Set<SettlementLedger>().AsNoTracking()
            .Where(l => l.CreatedAt >= periodStart && l.CreatedAt < periodEnd)
            .Select(l => new { l.InstructorId, l.Amount })
            .ToListAsync();

        var totals = rows
            .GroupBy(r => r.InstructorId)
            .Select(g => (InstructorId: g.Key, Amount: g.Sum(r => r.Amount)))
            .Where(t => t.Amount != 0m && !alreadyClosed.Contains(t.InstructorId))
            .ToList();

        foreach (var total in totals)
        {
            _db.Set<SettlementStatement>().Add(new SettlementStatement
            {
                InstructorId = total.InstructorId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GrossAmount = total.Amount,
                NetAmount = total.Amount,
            });
        }

        if (totals.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return totals.Count;
    }

    /// <summary>Total credited (sum of all ledger entries, including reversals).</summary>
    public async Task<decimal> GetTotalAsync(string instructorId)
    {
        return await _db.Set<SettlementLedger>()
            .Where(l => l.InstructorId == instructorId)
            .SumAsync(l => (decimal?)l.Amount) ?? 0m;
    }

    /// <summary>
    /// Available for withdrawal: total ledger minus amounts reserved by
    /// pending or already-paid withdrawals.
    /// </summary>
    public async Task<decimal> GetAvailableAsync(string instructorId)
    {
        var credits = await GetTotalAsync(instructorId);
        var reserved = await _db.Set<WithdrawalRequest>()
            .Where(w => w.InstructorId == instructorId
                && (w.Status == WithdrawalStatus.Pending || w.Status == WithdrawalStatus.Paid))
            .SumAsync(w => (decimal?)w.Amount) ?? 0m;
        return Math.Round(Math.Max(0m, credits - reserved), 2);
    }

    /// <summary>Earned amount per course, with titles resolved.</summary>
    public async Task<List<(int CourseId, string Title, decimal Amount)>> GetPerCourseAsync(string instructorId)
    {
        var rows = await _db.Set<SettlementLedger>().AsNoTracking()
            .Where(l => l.InstructorId == instructorId && l.CourseId != null)
            .Select(l => new { CourseId = l.CourseId!.Value, l.Amount })
            .ToListAsync();
        var courseIds = rows.Select(r => r.CourseId).Distinct().ToList();
        var titles = await _db.Set<Course>().AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title })
            .ToListAsync();
        var titleById = titles.ToDictionary(t => t.Id, t => t.Title);

        return rows
            .GroupBy(r => r.CourseId)
            .Select(g => (g.Key, titleById.GetValueOrDefault(g.Key) ?? $"Course #{g.Key}", g.Sum(r => r.Amount)))
            .OrderByDescending(r => r.Item3)
            .ToList();
    }

    /// <summary>Earned amount per calendar month (yyyy-MM).</summary>
    public async Task<List<(string Period, decimal Amount)>> GetPerPeriodAsync(string instructorId)
    {
        var rows = await _db.Set<SettlementLedger>().AsNoTracking()
            .Where(l => l.InstructorId == instructorId)
            .Select(l => new { l.CreatedAt, l.Amount })
            .ToListAsync();
        return rows
            .GroupBy(r => r.CreatedAt.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .Select(g => (g.Key, g.Sum(r => r.Amount)))
            .OrderByDescending(g => g.Key)
            .ToList();
    }

    public Task<List<SettlementLedger>> GetLedgerAsync(string instructorId)
    {
        return _db.Set<SettlementLedger>().AsNoTracking()
            .Where(l => l.InstructorId == instructorId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public Task<List<WithdrawalRequest>> GetWithdrawalsAsync(string instructorId)
    {
        return _db.Set<WithdrawalRequest>().AsNoTracking()
            .Where(w => w.InstructorId == instructorId)
            .OrderByDescending(w => w.RequestedAt)
            .ToListAsync();
    }

    /// <summary>Creates a pending withdrawal, reserving the balance.</summary>
    public async Task<(bool Ok, string? Error)> RequestWithdrawalAsync(string instructorId, decimal amount)
    {
        if (amount <= 0)
        {
            return (false, "Withdrawal amount must be positive.");
        }

        if (amount < MinWithdrawalAmount)
        {
            return (false, $"Withdrawals must be at least {MinWithdrawalAmount.ToString("C", CultureInfo.InvariantCulture)}.");
        }

        var available = await GetAvailableAsync(instructorId);
        if (available < MinWithdrawalAmount)
        {
            return (false, $"Your available balance must be at least {MinWithdrawalAmount.ToString("C", CultureInfo.InvariantCulture)} to withdraw.");
        }

        if (amount > available)
        {
            return (false, $"You can withdraw at most {available.ToString("C", CultureInfo.InvariantCulture)}.");
        }

        _db.Set<WithdrawalRequest>().Add(new WithdrawalRequest
        {
            InstructorId = instructorId,
            Amount = Math.Round(amount, 2),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Pending withdrawal requests for the admin review queue.</summary>
    public Task<List<WithdrawalRequest>> ListPendingAsync()
    {
        return _db.Set<WithdrawalRequest>().AsNoTracking()
            .Where(w => w.Status == WithdrawalStatus.Pending)
            .OrderBy(w => w.RequestedAt)
            .ToListAsync();
    }

    public Task<WithdrawalRequest?> GetByIdAsync(int id)
    {
        return _db.Set<WithdrawalRequest>().AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    /// <summary>Admin marks a withdrawal paid or rejected.</summary>
    public async Task<(bool Ok, string? Error)> ReviewAsync(int requestId, bool approve, string reviewerId)
    {
        var request = await _db.Set<WithdrawalRequest>()
            .FirstOrDefaultAsync(w => w.Id == requestId);
        if (request is null)
        {
            return (false, "Withdrawal request not found.");
        }

        if (request.Status != WithdrawalStatus.Pending)
        {
            return (false, "This withdrawal was already reviewed.");
        }

        request.Status = approve ? WithdrawalStatus.Paid : WithdrawalStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = reviewerId;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
