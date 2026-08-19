using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AsyncIO.Services;
using OpenLearning.CourseOutlineIO.Services;

namespace OpenLearning.CourseOutlineIO;

public static class CourseOutlineIOModuleExtensions
{
    public static IServiceCollection AddCourseOutlineIOModule(this IServiceCollection services)
    {
        services.AddScoped<OutlineImportService>();
        services.AddScoped<IAsyncIOProcessor>(sp => sp.GetRequiredService<OutlineImportService>());
        services.AddScoped<OutlineExportService>();
        return services;
    }
}
