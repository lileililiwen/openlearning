using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Jobs.Services;

namespace OpenLearning.Jobs;

public static class JobsModuleExtensions
{
    public static IServiceCollection AddJobsModule(this IServiceCollection services)
    {
        services.AddScoped<JobStore>();
        services.AddScoped<JobResolver>();
        services.AddScoped<JobDispatcher>();
        services.AddScoped<JobAdminService>();
        services.AddHostedService<JobScheduler>();
        return services;
    }

    /// <summary>Registers an <see cref="IJob"/> implementation with the scheduler.</summary>
    public static IServiceCollection AddJob<T>(this IServiceCollection services)
        where T : class, IJob
    {
        services.AddScoped<T>();
        services.AddScoped<IJob>(sp => sp.GetRequiredService<T>());
        return services;
    }
}
