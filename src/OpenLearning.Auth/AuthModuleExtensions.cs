using Microsoft.Extensions.DependencyInjection;
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireStudent, p => p.RequireRole(Roles.Student));
            options.AddPolicy(Policies.RequireInstructor, p => p.RequireRole(Roles.Instructor));
            options.AddPolicy(Policies.RequireAdmin, p => p.RequireRole(Roles.Admin));
        });

        services.AddScoped<AccountService>();
        return services;
    }
}
