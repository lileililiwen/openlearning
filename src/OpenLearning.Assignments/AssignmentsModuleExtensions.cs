using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Assignments.Services;

namespace OpenLearning.Assignments;

public static class AssignmentsModuleExtensions
{
    public static IServiceCollection AddAssignmentsModule(this IServiceCollection services)
    {
        services.AddScoped<AssignmentService>();
        return services;
    }
}
