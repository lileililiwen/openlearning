using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Exams.Services;

namespace OpenLearning.Exams;

public static class ExamsModuleExtensions
{
    public static IServiceCollection AddExamsModule(this IServiceCollection services)
    {
        services.AddScoped<ExamService>();
        services.AddScoped<ExamIntegrityService>();
        return services;
    }
}
