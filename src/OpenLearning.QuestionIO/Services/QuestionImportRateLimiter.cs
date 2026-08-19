using System.Collections.Concurrent;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.QuestionIO.Services;

public sealed record RateLimitCheck(bool Allowed, int RetryAfterSeconds);

/// <summary>
/// Per-user sliding-window rate limit for question imports. The limit and an
/// admin override list come from system-config. Attempt timestamps are kept in
/// a static dictionary so the limit holds across requests in a single instance;
/// each hour the window slides and stale entries are pruned.
/// </summary>
public class QuestionImportRateLimiter
{
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _attempts = new();

    private readonly SystemConfigService _config;

    public QuestionImportRateLimiter(SystemConfigService config)
    {
        _config = config;
    }

    /// <summary>Records an attempt and reports whether it is allowed.</summary>
    public async Task<RateLimitCheck> CheckAsync(string userId)
    {
        var limit = await _config.GetIntAsync("question.import.rateLimitPerHour", 5);
        var overrideCsv = await _config.GetStringAsync("question.import.rateLimitOverrideUserIds", string.Empty);
        if (overrideCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(userId, StringComparer.Ordinal))
        {
            return new RateLimitCheck(true, 0);
        }

        var now = DateTime.UtcNow;
        var queue = _attempts.GetOrAdd(userId, _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() <= now.AddHours(-1))
            {
                queue.Dequeue();
            }

            if (queue.Count >= limit)
            {
                var retryAfter = Math.Max(1, (int)Math.Ceiling((queue.Peek().AddHours(1) - now).TotalSeconds));
                return new RateLimitCheck(false, retryAfter);
            }

            queue.Enqueue(now);
            return new RateLimitCheck(true, 0);
        }
    }

    /// <summary>Clears recorded attempts (used by tests).</summary>
    public static void Reset()
    {
        _attempts.Clear();
    }
}
