using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Ecommerce;

public static class EcommerceModuleExtensions
{
    public static IServiceCollection AddEcommerceModule(this IServiceCollection services)
    {
        services.AddScoped<OrderService>();
        services.AddScoped<CartService>();
        services.AddScoped<CouponService>();
        services.AddScoped<LedgerService>();
        services.AddScoped<InvoiceService>();
        return services;
    }
}
