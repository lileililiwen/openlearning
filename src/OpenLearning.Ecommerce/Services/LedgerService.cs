using Microsoft.EntityFrameworkCore;
using OpenLearning.Ecommerce.Models;

namespace OpenLearning.Ecommerce.Services;

/// <summary>Account-balance and loyalty-points ledgers. Balances are running sums.</summary>
public class LedgerService
{
    private readonly DbContext _db;

    public LedgerService(DbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBalanceAsync(string userId)
    {
        return await _db.Set<BalanceLedger>()
            .Where(l => l.UserId == userId)
            .SumAsync(l => (decimal?)l.Amount) ?? 0m;
    }

    public async Task<int> GetPointsAsync(string userId)
    {
        return await _db.Set<PointsLedger>()
            .Where(l => l.UserId == userId)
            .SumAsync(l => (int?)l.Amount) ?? 0;
    }

    public async Task AddBalanceAsync(string userId, decimal amount, string reason)
    {
        _db.Set<BalanceLedger>().Add(new BalanceLedger { UserId = userId, Amount = amount, Reason = reason });
        await _db.SaveChangesAsync();
    }

    public async Task AddPointsAsync(string userId, int amount, string reason)
    {
        _db.Set<PointsLedger>().Add(new PointsLedger { UserId = userId, Amount = amount, Reason = reason });
        await _db.SaveChangesAsync();
    }
}
