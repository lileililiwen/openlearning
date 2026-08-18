using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Operations.Services;

namespace OpenLearning.Operations;

public static class OperationsModuleExtensions
{
    public static IServiceCollection AddOperationsModule(this IServiceCollection services)
    {
        services.AddScoped<OperationsService>();
        return services;
    }
}
