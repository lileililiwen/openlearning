using Microsoft.Extensions.DependencyInjection;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.UserManagement;

public static class UserManagementModuleExtensions
{
    public static IServiceCollection AddUserManagementModule(this IServiceCollection services)
    {
        services.AddScoped<UserManagementService>();
        services.AddScoped<InstructorApplicationService>();
        return services;
    }
}
