using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Competency.Services;

namespace OpenLearning.Competency;

public static class CompetencyModuleExtensions
{
    public static IServiceCollection AddCompetencyModule(this IServiceCollection services)
    {
        services.AddScoped<CompetencyService>();
        return services;
    }
}
