using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Authorization;

public class NotSuspendedHandler : AuthorizationHandler<NotSuspendedRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public NotSuspendedHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, NotSuspendedRequirement requirement)
    {
        // Read from the database so suspension takes effect on the next request.
        var user = await _userManager.GetUserAsync(context.User);
        if (user is null || user.IsSuspended)
        {
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}
