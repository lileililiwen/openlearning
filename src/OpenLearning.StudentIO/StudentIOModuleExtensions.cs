using Microsoft.Extensions.DependencyInjection;
using OpenLearning.AsyncIO.Services;
using OpenLearning.StudentIO.Services;

namespace OpenLearning.StudentIO;

/// <summary>Registers the student bulk-import module services.</summary>
public static class StudentIOModuleExtensions
{
    public static IServiceCollection AddStudentIOModule(this IServiceCollection services)
    {
        services.AddScoped<StudentImportService>();
        services.AddScoped<IAsyncIOProcessor>(sp => sp.GetRequiredService<StudentImportService>());
        return services;
    }
}
