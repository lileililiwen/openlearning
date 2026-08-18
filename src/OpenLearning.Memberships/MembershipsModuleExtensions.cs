using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Memberships.Services;

namespace OpenLearning.Memberships;

public static class MembershipsModuleExtensions
{
    public static IServiceCollection AddMembershipsModule(this IServiceCollection services)
    {
        services.AddScoped<MembershipService>();
        return services;
    }
}
