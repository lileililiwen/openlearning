using Microsoft.Extensions.Configuration;
using OpenLearning.Distribution.Services;
using OpenLearning.Jobs;

namespace OpenLearning.Distribution.Jobs;

/// <summary>
/// Transitions commissions past the holding period to Available.
/// Idempotency: only Pending entries older than the holding period are touched.
/// </summary>
public sealed class DistributionHoldExpireJob : IJob
{
    public string Key => "distribution.commissions.hold-expire";

    public string Cron => "0 2 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(15);

    private readonly DistributionService _distribution;
    private readonly IConfiguration _config;

    public DistributionHoldExpireJob(DistributionService distribution, IConfiguration config)
    {
        _distribution = distribution;
        _config = config;
    }

    public Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var holdingDays = Math.Clamp(_config.GetValue("Distribution:HoldingDays", 7), 1, 90);
        return _distribution.TransitionHeldAsync(TimeSpan.FromDays(holdingDays));
    }
}

/// <summary>
/// Closes the current period and creates an immutable settlement statement per
/// distributor. Idempotent: distributors with a statement for the same period
/// are skipped.
/// </summary>
public sealed class DistributionSettlementCloseJob : IJob
{
    public string Key => "distribution.settlement.close-period";

    public string Cron => "0 23 * * 0";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly DistributionService _distribution;

    public DistributionSettlementCloseJob(DistributionService distribution)
    {
        _distribution = distribution;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var start = now.Date.AddDays(-daysSinceMonday);
        await _distribution.ClosePeriodAsync(start, start.AddDays(7));
    }
}
