using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Certificates.Services;

namespace OpenLearning.Certificates;

public static class CertificatesModuleExtensions
{
    public static IServiceCollection AddCertificatesModule(this IServiceCollection services)
    {
        services.AddScoped<CertificateService>();
        return services;
    }
}
