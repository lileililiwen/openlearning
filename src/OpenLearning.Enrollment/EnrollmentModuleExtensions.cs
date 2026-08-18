using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Enrollment;

public static class EnrollmentModuleExtensions
{
    public static IServiceCollection AddEnrollmentModule(this IServiceCollection services)
    {
        services.AddScoped<EnrollmentService>();
        return services;
    }
}
