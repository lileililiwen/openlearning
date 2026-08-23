using Microsoft.AspNetCore.Authorization;
using OpenLearning.Organizations.Models;
using OpenLearning.Organizations.Services;

namespace OpenLearning.Organizations.Authorization;

public sealed class OrganizationRoleRequirement(params OrganizationRole[] roles) : IAuthorizationRequirement
{
    public IReadOnlySet<OrganizationRole> Roles { get; } = roles.ToHashSet();
}

public sealed class OrganizationRoleHandler(IOrganizationContext organizationContext)
    : AuthorizationHandler<OrganizationRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationRoleRequirement requirement)
    {
        var active = await organizationContext.GetActiveAsync();
        if (active is not null && requirement.Roles.Contains(active.Role))
        {
            context.Succeed(requirement);
        }
    }
}

public static class OrganizationPolicies
{
    public const string Member = "OrganizationMember";
    public const string Admin = "OrganizationAdmin";
}
