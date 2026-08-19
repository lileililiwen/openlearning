using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Navigation.Services;

namespace OpenLearning.Navigation;

public static class NavigationModuleExtensions
{
    public static IServiceCollection AddNavigationModule(this IServiceCollection services)
    {
        services.AddScoped<MenuService>();
        services.AddScoped<BreadcrumbService>();
        services.AddScoped<NavCounterService>();
        services.AddScoped<NavPreferencesService>();
        return services;
    }
}
