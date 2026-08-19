using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Services;

namespace OpenLearning.Classes;

public static class ClassesModuleExtensions
{
    public static IServiceCollection AddClassesModule(this IServiceCollection services)
    {
        services.AddScoped<ClassGroupService>();
        services.AddScoped<ClassAssignmentService>();
        services.AddScoped<ClassRosterService>();
        // Registered after AddAuthModule so this overrides NullClassAssignmentLookup.
        services.AddScoped<IClassAssignmentLookup, ClassAssignmentLookup>();
        return services;
    }
}
