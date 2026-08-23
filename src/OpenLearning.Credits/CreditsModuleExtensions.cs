using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Credits.Services;

namespace OpenLearning.Credits;

public static class CreditsModuleExtensions
{
    public static IServiceCollection AddCreditsModule(this IServiceCollection services)
    {
        services.AddScoped<CreditService>();
        return services;
    }
}
