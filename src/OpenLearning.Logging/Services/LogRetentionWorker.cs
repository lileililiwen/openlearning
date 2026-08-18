using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenLearning.Logging.Services;

namespace OpenLearning.Logging.Services;

/// <summary>
/// Periodically prunes log rows older than the configured retention period so
/// the tables stay bounded.
/// </summary>
public sealed class LogRetentionWorker : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromHours(24);

    private static readonly Action<ILogger, int, int, Exception?> _logPruned = LoggerMessage.Define<int, int>(
        LogLevel.Information, 1, "Pruned {Count} log rows older than {Days} days.");

    private static readonly Action<ILogger, Exception?> _logPruneFailed = LoggerMessage.Define(
        LogLevel.Warning, 2, "Log retention prune failed.");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _retentionDays;
    private readonly ILogger<LogRetentionWorker> _logger;

    public LogRetentionWorker(
        IServiceScopeFactory scopeFactory,
        int retentionDays,
        ILogger<LogRetentionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _retentionDays = retentionDays;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var logs = scope.ServiceProvider.GetRequiredService<LogService>();
                var removed = await logs.PruneAsync(_retentionDays);
                if (removed > 0)
                {
                    _logPruned(_logger, removed, _retentionDays, null);
                }
            }
            catch (Exception ex)
            {
                _logPruneFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
