using Microsoft.EntityFrameworkCore;
using OpenLearning.Integrations.Models;

namespace OpenLearning.UnitTests.Integrations;

/// <summary>Maps only the Integrations entities against an in-memory store for fast unit tests.</summary>
internal sealed class IntegrationsTestContext : DbContext
{
    public IntegrationsTestContext(DbContextOptions<IntegrationsTestContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationRegistration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
