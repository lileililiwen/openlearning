using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;
using OpenLearning.Integrations.Services;
using Xunit;

namespace OpenLearning.UnitTests.Integrations;

/// <summary>
/// Maps only the Integrations entities against PostgreSQL to exercise the relational
/// provider (unique indexes, persistence, tenant scoping) that the InMemory provider
/// ignores. Uses the dedicated local <c>openlearning_integration_integrations</c> database.
/// </summary>
internal sealed class IntegrationsPostgresContext : DbContext
{
    public IntegrationsPostgresContext(DbContextOptions<IntegrationsPostgresContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationRegistration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class IntegrationOperationsServicePostgresTests
{
    private const string _connectionString =
        "Host=localhost;Database=openlearning_integration_integrations;Username=openlearning;Password=openlearning_dev";

    private static async Task<IntegrationsPostgresContext> NewDbAsync()
    {
        var options = new DbContextOptionsBuilder<IntegrationsPostgresContext>()
            .UseNpgsql(_connectionString)
            .Options;
        var db = new IntegrationsPostgresContext(options);
        // Start from a clean database so unique indexes never collide with leftovers
        // from a previous test run against the shared local integration database.
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<int> SeedRegistrationAsync(IntegrationsPostgresContext db, string tenant)
    {
        var registration = new IntegrationRegistration
        {
            Name = "pg-test",
            Type = "Webhook",
            ContractVersion = 1,
            TenantId = tenant,
            IsEnabled = true,
            EndpointReference = "https://example.com/hook",
        };
        db.Set<IntegrationRegistration>().Add(registration);
        await db.SaveChangesAsync();
        return registration.Id;
    }

    [Fact]
    public async Task DeclareAndDeliver_PersistRelationally()
    {
        await using var db = await NewDbAsync();
        var adapter = new FakePostgresAdapter(IntegrationResult.Success());
        var service = new IntegrationOperationsService(db, new[] { adapter });
        var registrationId = await SeedRegistrationAsync(db, "tenant-1");

        var result = await service.DeliverAsync("tenant-1", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "pg-1",
            Payload = "{\"event\":\"grade.posted\"}",
        });

        Assert.Equal(IntegrationDeliveryStatus.Delivered, result.Status);
        var stored = await db.Set<IntegrationDelivery>().AsNoTracking().SingleAsync();
        Assert.Equal("pg-1", stored.IdempotencyKey);
        Assert.Equal(IntegrationDeliveryStatus.Delivered, stored.Status);
    }

    [Fact]
    public async Task UniqueIdempotencyIndex_EnforcedByProvider()
    {
        await using var db = await NewDbAsync();
        await SeedRegistrationAsync(db, "tenant-1");

        db.Set<IntegrationDelivery>().Add(new IntegrationDelivery
        {
            RegistrationId = 1,
            IdempotencyKey = "dup",
            EventType = "e",
            PayloadReference = "x",
            TenantId = "tenant-1",
        });
        db.Set<IntegrationDelivery>().Add(new IntegrationDelivery
        {
            RegistrationId = 1,
            IdempotencyKey = "dup",
            EventType = "e",
            PayloadReference = "x",
            TenantId = "tenant-1",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task TenantScoping_ReplayAcrossTenantThrows()
    {
        await using var db = await NewDbAsync();
        var adapter = new FakePostgresAdapter(IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "down"));
        var service = new IntegrationOperationsService(db, new[] { adapter }, maxAttempts: 3);
        var registrationId = await SeedRegistrationAsync(db, "tenant-A");

        var first = await service.DeliverAsync("tenant-A", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "pg-replay",
            Payload = "{}",
        });
        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, first.Status);

        await Assert.ThrowsAsync<UnauthorizedIntegrationOperationException>(
            () => service.ReplayDeadLetterAsync("tenant-B", "actor", first.DeliveryId!.Value));
    }

    [Fact]
    public async Task DeadLetter_PersistsRelationally()
    {
        await using var db = await NewDbAsync();
        var adapter = new FakePostgresAdapter(IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "unreachable"));
        var service = new IntegrationOperationsService(db, new[] { adapter }, maxAttempts: 3);
        var registrationId = await SeedRegistrationAsync(db, "tenant-1");

        var result = await service.DeliverAsync("tenant-1", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "pg-dead",
            Payload = "{}",
        });

        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, result.Status);
        var stored = await db.Set<IntegrationDelivery>().AsNoTracking().SingleAsync();
        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, stored.Status);
    }

    private sealed class FakePostgresAdapter : IIntegrationAdapter
    {
        private readonly IntegrationResult _result;

        public FakePostgresAdapter(IntegrationResult result)
        {
            _result = result;
        }

        public bool SupportsType(string integrationType)
        {
            return true;
        }

        public Task<IntegrationResult> ValidateAsync(IntegrationRegistration registration, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IntegrationResult.Success());
        }

        public Task<IntegrationResult> ExecuteAsync(IntegrationRegistration registration, IntegrationDeliveryRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }

        public Task<IntegrationResult> HealthAsync(IntegrationRegistration registration, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IntegrationResult.Success());
        }
    }
}
