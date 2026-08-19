using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Invoicing.Services;

namespace OpenLearning.Invoicing;

public static class InvoicingModuleExtensions
{
    public static IServiceCollection AddInvoicingModule(this IServiceCollection services)
    {
        services.AddScoped<InvoiceNumberService>();
        services.AddScoped<InvoiceService>();
        return services;
    }
}
