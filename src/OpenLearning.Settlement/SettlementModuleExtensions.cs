using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Settlement.Services;

namespace OpenLearning.Settlement;

public static class SettlementModuleExtensions
{
    public static IServiceCollection AddSettlementModule(this IServiceCollection services)
    {
        services.AddScoped<SettlementService>();
        return services;
    }
}
