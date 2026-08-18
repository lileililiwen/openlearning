using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenLearning.Data;

/// <summary>
/// Allows EF Core tools to create the DbContext at design time without
/// bootstrapping the full web application.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Design-time factory used only by `dotnet ef` against the local dev database.
        // S2068: the fallback credential is the documented local PostgreSQL dev database
        // (Agents.md §4.2) and can be overridden via OPENLEARNING_CONNECTION.
#pragma warning disable S2068
        var connection = Environment.GetEnvironmentVariable("OPENLEARNING_CONNECTION")
            ?? "Host=localhost;Database=openlearning;Username=openlearning;Password=openlearning_dev";
#pragma warning restore S2068
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new ApplicationDbContext(options);
    }
}
