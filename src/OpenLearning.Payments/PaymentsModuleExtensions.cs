using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Payments.Services;

namespace OpenLearning.Payments;

public static class PaymentsModuleExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PaymentOptions>(config.GetSection("Payments"));
        services.AddScoped<IPaymentProvider, SandboxPaymentProvider>();
        services.AddScoped<PaymentService>();
        return services;
    }
}
