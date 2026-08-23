using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Organizations.Authorization;
using OpenLearning.Organizations.Models;
using OpenLearning.Organizations.Services;

namespace OpenLearning.Organizations;

public static class OrganizationsModuleExtensions
{
    public static IServiceCollection AddOrganizationsModule(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IOrganizationContext, OrganizationContext>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<IAuthorizationHandler, OrganizationRoleHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(OrganizationPolicies.Member, p => p.RequireAuthenticatedUser().AddRequirements(new OrganizationRoleRequirement(Enum.GetValues<OrganizationRole>())));
            options.AddPolicy(OrganizationPolicies.Admin, p => p.RequireAuthenticatedUser().AddRequirements(new OrganizationRoleRequirement(OrganizationRole.OrganizationAdmin)));
        });
        return services;
    }
}
