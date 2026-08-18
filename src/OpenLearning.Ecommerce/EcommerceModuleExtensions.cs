using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Ecommerce;

public static class EcommerceModuleExtensions
{
    public static IServiceCollection AddEcommerceModule(this IServiceCollection services)
    {
        services.AddScoped<OrderService>();
        return services;
    }
}
