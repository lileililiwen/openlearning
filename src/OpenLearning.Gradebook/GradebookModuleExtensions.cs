using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Gradebook.Services;

namespace OpenLearning.Gradebook;

public static class GradebookModuleExtensions
{
    public static IServiceCollection AddGradebookModule(this IServiceCollection services)
    {
        services.AddScoped<GradebookService>();
        return services;
    }
}
