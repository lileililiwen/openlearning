using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenLearning.Jobs.Services;

/// <summary>
/// Background loop that recovers stale runs on startup, registers the IJob
/// registry, and ticks every 30 seconds to dispatch due jobs.
/// </summary>
public sealed class JobScheduler : BackgroundService
{
    private static readonly TimeSpan _tickInterval = TimeSpan.FromSeconds(30);

    private static readonly Action<ILogger, int, Exception?> _logInitialized = LoggerMessage.Define<int>(
        LogLevel.Information, 1, "Job scheduler initialized with {Count} registered jobs.");

    private static readonly Action<ILogger, Exception?> _logTickFailed = LoggerMessage.Define(
        LogLevel.Warning, 2, "Job scheduler tick failed.");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(IServiceScopeFactory scopeFactory, ILogger<JobScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logTickFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(_tickInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task InitializeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<JobStore>();
        var resolver = scope.ServiceProvider.GetRequiredService<JobResolver>();
        await store.RecoverStaleRunsAsync();
        foreach (var job in resolver.All())
        {
            await store.EnsureRegisteredAsync(job.Key, job.Cron, DateTime.UtcNow);
        }

        _logInitialized(_logger, resolver.All().Count, null);
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<JobStore>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<JobDispatcher>();
        var now = DateTime.UtcNow;
        var due = await store.GetDueAsync(now);
        foreach (var job in due)
        {
            await dispatcher.RunDueAsync(job, now, stoppingToken);
        }
    }
}
