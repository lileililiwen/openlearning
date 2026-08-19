using System.Collections.Concurrent;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.CouponIO.Services;

public sealed record CouponRateLimitCheck(bool Allowed, int RetryAfterSeconds);

/// <summary>
/// Per-admin sliding-window rate limit for coupon imports
/// (<c>coupon.import.rateLimitPerHour</c>, default 5). Attempt timestamps are
/// kept in a static dictionary so the limit holds across requests; the window
/// slides each hour and stale entries are pruned.
/// </summary>
public class CouponImportRateLimiter
{
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _attempts = new();

    private readonly SystemConfigService _config;

    public CouponImportRateLimiter(SystemConfigService config)
    {
        _config = config;
    }

    /// <summary>Records an attempt and reports whether it is allowed.</summary>
    public async Task<CouponRateLimitCheck> CheckAsync(string adminId)
    {
        var limit = await _config.GetIntAsync("coupon.import.rateLimitPerHour", 5);
        var now = DateTime.UtcNow;
        var queue = _attempts.GetOrAdd(adminId, _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() <= now.AddHours(-1))
            {
                queue.Dequeue();
            }

            if (queue.Count >= limit)
            {
                var retryAfter = Math.Max(1, (int)Math.Ceiling((queue.Peek().AddHours(1) - now).TotalSeconds));
                return new CouponRateLimitCheck(false, retryAfter);
            }

            queue.Enqueue(now);
            return new CouponRateLimitCheck(true, 0);
        }
    }

    /// <summary>Clears recorded attempts (used by tests).</summary>
    public static void Reset()
    {
        _attempts.Clear();
    }
}
