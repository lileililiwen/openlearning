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
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=openlearning;Username=openlearning;Password=openlearning_dev")
            .Options;

        return new ApplicationDbContext(options);
    }
}
