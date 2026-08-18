using Microsoft.EntityFrameworkCore;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Services;

/// <summary>Stores and prunes browser web-push subscriptions per user.</summary>
public class PushSubscriptionService
{
    private readonly DbContext _db;

    public PushSubscriptionService(DbContext db)
    {
        _db = db;
    }

    public async Task SubscribeAsync(
        string userId,
        string endpoint,
        string p256dh,
        string auth)
    {
        var existing = await _db.Set<PushSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);
        if (existing is not null)
        {
            existing.P256Dh = p256dh;
            existing.Auth = auth;
        }
        else
        {
            _db.Set<PushSubscription>().Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256Dh = p256dh,
                Auth = auth,
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<bool> UnsubscribeAsync(string userId, string endpoint)
    {
        var subscription = await _db.Set<PushSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);
        if (subscription is null)
        {
            return false;
        }

        _db.Set<PushSubscription>().Remove(subscription);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<PushSubscription>> GetForUserAsync(string userId)
    {
        return _db.Set<PushSubscription>().AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}
