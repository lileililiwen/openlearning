using Microsoft.Extensions.DependencyInjection;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.CourseManagement;

public static class CourseManagementModuleExtensions
{
    public static IServiceCollection AddCourseManagementModule(this IServiceCollection services)
    {
        services.AddScoped<CourseService>();
        services.AddScoped<ModuleService>();
        services.AddScoped<LessonService>();
        services.AddScoped<TagService>();
        services.AddScoped<CategoryService>();
        return services;
    }
}
