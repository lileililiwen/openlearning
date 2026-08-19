using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Logging.Services;

namespace OpenLearning.Logging;

public static class LoggingModuleExtensions
{
    /// <summary>
    /// Registers the log service. Log pruning now happens via the
    /// `logs.archive` scheduled job (LogArchiveJob) instead of an embedded
    /// background worker.
    /// </summary>
    public static IServiceCollection AddLoggingModule(this IServiceCollection services)
    {
        services.AddScoped<LogService>();
        return services;
    }
}
