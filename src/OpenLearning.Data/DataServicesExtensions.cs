using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenLearning.Data;

public static class DataServicesExtensions
{
    /// <summary>
    /// Registers the central ApplicationDbContext. Also maps the base
    /// <see cref="DbContext"/> to the concrete context so that module services
    /// can depend on the base type without creating circular project references.
    /// </summary>
    public static IServiceCollection AddDataServices(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<DbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
