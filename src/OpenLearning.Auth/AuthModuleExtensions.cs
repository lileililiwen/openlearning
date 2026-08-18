using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Auth.Authorization;
using OpenLearning.Auth.Services;

namespace OpenLearning.Auth;

public static class AuthModuleExtensions
{
    /// <summary>
    /// Registers role policies and account services. Call after Identity itself
    /// has been added (AddIdentity ... AddEntityFrameworkStores) in the
    /// composition root, since this module intentionally avoids a dependency on
    /// the DbContext.
    /// </summary>
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.AccessDeniedPath = "/Auth/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
        });

        // Re-validate the security stamp on every request so admin role changes
        // and suspensions take effect immediately on the user's next request.
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        services.AddScoped<IAuthorizationHandler, NotSuspendedHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireStudent, p => p
                .RequireRole(Roles.Student)
                .AddRequirements(new NotSuspendedRequirement()));
            options.AddPolicy(Policies.RequireInstructor, p => p
                .RequireRole(Roles.Instructor)
                .AddRequirements(new NotSuspendedRequirement()));
            options.AddPolicy(Policies.RequireAdmin, p => p
                .RequireRole(Roles.Admin)
                .AddRequirements(new NotSuspendedRequirement()));
        });

        services.AddScoped<AccountService>();
        return services;
    }
}
