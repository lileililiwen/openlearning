using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Outcomes.Services;

namespace OpenLearning.Outcomes;

public static class OutcomesModuleExtensions
{
    public static IServiceCollection AddOutcomesModule(this IServiceCollection services)
    {
        services.AddScoped<OutcomeOperationsService>();
        return services;
    }
}
