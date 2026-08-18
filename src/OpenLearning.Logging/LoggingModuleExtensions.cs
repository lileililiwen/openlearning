using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenLearning.Logging.Services;

namespace OpenLearning.Logging;

public static class LoggingModuleExtensions
{
    public static IServiceCollection AddLoggingModule(this IServiceCollection services, int retentionDays = 90)
    {
        services.AddScoped<LogService>();
        services.AddHostedService(sp => new LogRetentionWorker(
            sp.GetRequiredService<IServiceScopeFactory>(),
            retentionDays,
            sp.GetRequiredService<ILogger<LogRetentionWorker>>()));
        return services;
    }
}
