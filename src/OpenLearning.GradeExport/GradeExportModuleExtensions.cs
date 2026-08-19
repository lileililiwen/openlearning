using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AsyncIO.Services;
using OpenLearning.GradeExport.Services;

namespace OpenLearning.GradeExport;

public static class GradeExportModuleExtensions
{
    public static IServiceCollection AddGradeExportModule(this IServiceCollection services)
    {
        services.AddScoped<GradeExportService>();
        services.AddScoped<IAsyncIOProcessor>(sp => sp.GetRequiredService<GradeExportService>());
        return services;
    }
}
