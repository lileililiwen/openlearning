using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AsyncIO.Services;
using OpenLearning.CouponIO.Services;

namespace OpenLearning.CouponIO;

public static class CouponIOModuleExtensions
{
    public static IServiceCollection AddCouponIOModule(this IServiceCollection services)
    {
        services.AddScoped<CouponImportRateLimiter>();
        services.AddScoped<CouponImportService>();
        services.AddScoped<IAsyncIOProcessor>(sp => sp.GetRequiredService<CouponImportService>());
        return services;
    }
}
