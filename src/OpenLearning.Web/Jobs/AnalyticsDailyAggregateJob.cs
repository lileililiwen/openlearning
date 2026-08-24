using OpenLearning.Analytics.Services;
using OpenLearning.Jobs;

namespace OpenLearning.Web.Jobs;

/// <summary>
/// Aggregates the previous UTC day's learning events into course/cohort/
/// assessment/workload facts. Atomic: facts are only served once the run is
/// marked succeeded.
/// </summary>
public sealed class AnalyticsDailyAggregateJob : IJob
{
    public string Key => "analytics.daily-aggregate";

    public string Cron => "0 4 * * *";

    public TimeSpan Timeout => TimeSpan.FromMinutes(30);

    private readonly AnalyticsAggregateService _aggregates;

    public AnalyticsDailyAggregateJob(AnalyticsAggregateService aggregates)
    {
        _aggregates = aggregates;
    }

    public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        await _aggregates.RefreshDailyAsync(yesterday);
    }
}
