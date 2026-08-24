using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Analytics.Services;

namespace OpenLearning.Analytics;

public static class AnalyticsModuleExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddScoped<LearningEventService>();
        services.AddScoped<AnalyticsAggregateService>();
        services.AddScoped<AnalyticsReportService>();
        return services;
    }
}
