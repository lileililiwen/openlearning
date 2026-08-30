using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;
using OpenLearning.Integrations.Services;
using Xunit;

namespace OpenLearning.UnitTests.Integrations;

/// <summary>A configurable adapter used to exercise delivery outcomes without network calls.</summary>
internal sealed class FakeIntegrationAdapter : IIntegrationAdapter
{
    private readonly Func<IntegrationResult> _resultFactory;

    public int ExecuteCalls { get; private set; }

    public bool ValidateSucceeds { get; set; } = true;

    public FakeIntegrationAdapter(Func<IntegrationResult> resultFactory)
    {
        _resultFactory = resultFactory;
    }

    public bool SupportsType(string integrationType)
    {
        return true;
    }

    public Task<IntegrationResult> ValidateAsync(IntegrationRegistration registration, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidateSucceeds ? IntegrationResult.Success() : IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "validation"));
    }

    public Task<IntegrationResult> ExecuteAsync(IntegrationRegistration registration, IntegrationDeliveryRequest request, System.Threading.CancellationToken cancellationToken = default)
    {
        ExecuteCalls++;
        return Task.FromResult(_resultFactory());
    }

    public Task<IntegrationResult> HealthAsync(IntegrationRegistration registration, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IntegrationResult.Success());
    }
}

public sealed class IntegrationOperationsServiceTests
{
    private static IntegrationsTestContext NewDb()
    {
        return new IntegrationsTestContext(
            new DbContextOptionsBuilder<IntegrationsTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static async Task<int> SeedRegistrationAsync(IntegrationsTestContext db, string type, string tenant = "platform", bool enabled = true, int contractVersion = 1)
    {
        var registration = new IntegrationRegistration
        {
            Name = "test",
            Type = type,
            ContractVersion = contractVersion,
            TenantId = tenant,
            IsEnabled = enabled,
            EndpointReference = "https://example.com/hook",
        };
        db.Set<IntegrationRegistration>().Add(registration);
        await db.SaveChangesAsync();
        return registration.Id;
    }

    [Fact]
    public async Task DeclareIntegrationAsync_StoresRegistrationWithTenant()
    {
        await using var db = NewDb();
        var service = new IntegrationOperationsService(db);

        var result = await service.DeclareIntegrationAsync("tenant-1", "actor-1", new DeclareIntegrationRequest
        {
            Name = "Gradebook webhook",
            Type = "Webhook",
            ContractVersion = 1,
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Registration);
        var stored = await db.Set<IntegrationRegistration>().AsNoTracking().SingleAsync();
        Assert.Equal("tenant-1", stored.TenantId);
        Assert.True(stored.IsEnabled);
    }

    [Fact]
    public async Task DeclareIntegrationAsync_RejectsUnsupportedType()
    {
        await using var db = NewDb();
        var service = new IntegrationOperationsService(db);

        var result = await service.DeclareIntegrationAsync("tenant-1", "actor-1", new DeclareIntegrationRequest
        {
            Name = "x",
            Type = "SmokeSignal",
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == "Type");
    }

    [Fact]
    public async Task DeclareIntegrationAsync_RejectsInvalidContractVersion()
    {
        await using var db = NewDb();
        var service = new IntegrationOperationsService(db);

        var result = await service.DeclareIntegrationAsync("tenant-1", "actor-1", new DeclareIntegrationRequest
        {
            Name = "x",
            Type = "Webhook",
            ContractVersion = 0,
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == nameof(DeclareIntegrationRequest.ContractVersion));
    }

    [Fact]
    public async Task DeliverAsync_UnknownContractVersion_FailsClosedWithoutExternalCall()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Success());
        var service = new IntegrationOperationsService(db, new[] { adapter });
        var registrationId = await SeedRegistrationAsync(db, "Webhook", contractVersion: 99);

        var result = await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "k1",
            Payload = "{}",
        });

        Assert.Equal(IntegrationDeliveryStatus.Failed, result.Status);
        Assert.Equal(IntegrationErrorCategory.UnknownVersion, result.ErrorCategory);
        Assert.Equal(0, adapter.ExecuteCalls);
        var audit = await db.Set<IntegrationAuditRecord>().AsNoTracking().SingleAsync();
        Assert.Equal("Dispatch", audit.Action);
        Assert.Equal("Failed", audit.Disposition);
    }

    [Fact]
    public async Task DeliverAsync_DisabledIntegration_FailsClosedWithoutExternalCall()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Success());
        var service = new IntegrationOperationsService(db, new[] { adapter });
        var registrationId = await SeedRegistrationAsync(db, "Webhook", enabled: false);

        var result = await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "k1",
            Payload = "{}",
        });

        Assert.Equal(IntegrationErrorCategory.Disabled, result.ErrorCategory);
        Assert.Equal(0, adapter.ExecuteCalls);
    }

    [Fact]
    public async Task DeliverAsync_DuplicateIdempotencyKey_ReturnsPriorDispositionWithoutExternalCall()
    {
        await using var db = NewDb();
        var firstAdapter = new FakeIntegrationAdapter(() => IntegrationResult.Success());
        var service = new IntegrationOperationsService(db, new[] { firstAdapter });
        var registrationId = await SeedRegistrationAsync(db, "Webhook");

        var first = await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "dup-key",
            Payload = "{}",
        });
        Assert.Equal(IntegrationDeliveryStatus.Delivered, first.Status);
        Assert.Equal(1, firstAdapter.ExecuteCalls);

        // Second delivery with the same key uses a fresh adapter; it must not be invoked.
        var secondAdapter = new FakeIntegrationAdapter(() => IntegrationResult.Success());
        var service2 = new IntegrationOperationsService(db, new[] { secondAdapter });
        var second = await service2.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "dup-key",
            Payload = "{}",
        });

        Assert.True(second.FromCache);
        Assert.Equal(IntegrationDeliveryStatus.Delivered, second.Status);
        Assert.Equal(0, secondAdapter.ExecuteCalls);
    }

    [Fact]
    public async Task DeliverAsync_ProviderTimeout_ExhaustsRetriesThenDeadLetters()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Failure(IntegrationErrorCategory.Timeout, "timed out"));
        var service = new IntegrationOperationsService(db, new[] { adapter }, maxAttempts: 3);
        var registrationId = await SeedRegistrationAsync(db, "Webhook");

        var result = await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "to1",
            Payload = "{}",
        });

        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, result.Status);
        Assert.Equal(3, adapter.ExecuteCalls);
        var delivery = await db.Set<IntegrationDelivery>().AsNoTracking().SingleAsync();
        Assert.Equal(3, delivery.Attempts);
        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, delivery.Status);
    }

    [Fact]
    public async Task DeliverAsync_PermanentFailure_DeadLettersImmediately()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "bad endpoint"));
        var service = new IntegrationOperationsService(db, new[] { adapter }, maxAttempts: 3);
        var registrationId = await SeedRegistrationAsync(db, "Webhook");

        var result = await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "perm1",
            Payload = "{}",
        });

        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, result.Status);
        Assert.Equal(1, adapter.ExecuteCalls);
    }

    [Fact]
    public async Task DeliverAsync_Success_StoresDeliveredAndAuditWithoutSecrets()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Success());
        var service = new IntegrationOperationsService(db, new[] { adapter });
        var registrationId = await SeedRegistrationAsync(db, "Webhook");

        var secret = "supersecrettokenabcdefghijklmnopqrstuvwxyz0123456789";
        var payload = $"{{\"token\":\"{secret}\",\"event\":\"grade.posted\"}}";
        await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "ok1",
            Payload = payload,
        });

        var delivery = await db.Set<IntegrationDelivery>().AsNoTracking().SingleAsync();
        Assert.Equal(IntegrationDeliveryStatus.Delivered, delivery.Status);
        Assert.DoesNotContain(secret, delivery.PayloadReference);
        Assert.Contains("***REDACTED***", delivery.PayloadReference);

        var audit = await db.Set<IntegrationAuditRecord>().AsNoTracking().SingleAsync(a => a.Action == "Dispatch");
        Assert.DoesNotContain(secret, audit.RedactedPayload ?? string.Empty);
        Assert.Contains("***REDACTED***", audit.RedactedPayload ?? string.Empty);
    }

    [Fact]
    public async Task SetEnabledAsync_TenantMismatch_ThrowsUnauthorized()
    {
        await using var db = NewDb();
        var service = new IntegrationOperationsService(db);
        var registrationId = await SeedRegistrationAsync(db, "Webhook", tenant: "tenant-A");

        await Assert.ThrowsAsync<UnauthorizedIntegrationOperationException>(
            () => service.SetEnabledAsync("tenant-B", "actor", registrationId, false));

        var stored = await db.Set<IntegrationRegistration>().AsNoTracking().SingleAsync();
        Assert.True(stored.IsEnabled);
    }

    [Fact]
    public async Task ReplayDeadLetterAsync_CreatesNewAttemptPreservingEventIdentity()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "down"));
        var service = new IntegrationOperationsService(db, new[] { adapter }, maxAttempts: 3);
        var registrationId = await SeedRegistrationAsync(db, "Webhook");
        var first = await service.DeliverAsync("platform", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "replay1",
            Payload = "{}",
        });
        Assert.Equal(IntegrationDeliveryStatus.DeadLettered, first.Status);

        var replayAdapter = new FakeIntegrationAdapter(() => IntegrationResult.Success());
        var service2 = new IntegrationOperationsService(db, new[] { replayAdapter });
        var replay = await service2.ReplayDeadLetterAsync("platform", "actor", first.DeliveryId!.Value);

        Assert.Equal(IntegrationDeliveryStatus.Delivered, replay.Status);
        var deliveries = await db.Set<IntegrationDelivery>().AsNoTracking().OrderBy(d => d.Id).ToListAsync();
        Assert.Equal(2, deliveries.Count);
        Assert.Equal("replay1", deliveries[0].IdempotencyKey);
        Assert.Equal("replay1", deliveries[1].IdempotencyKey);
        Assert.NotEqual(deliveries[0].Id, deliveries[1].Id);
        var replayAudit = await db.Set<IntegrationAuditRecord>().AsNoTracking().SingleAsync(a => a.Action == "Replay");
        Assert.Equal(first.DeliveryId.Value, replayAudit.DeliveryId);
    }

    [Fact]
    public async Task ReplayDeadLetterAsync_TenantMismatch_ThrowsUnauthorized()
    {
        await using var db = NewDb();
        var adapter = new FakeIntegrationAdapter(() => IntegrationResult.Failure(IntegrationErrorCategory.Permanent, "down"));
        var service = new IntegrationOperationsService(db, new[] { adapter }, maxAttempts: 3);
        var registrationId = await SeedRegistrationAsync(db, "Webhook", tenant: "tenant-A");
        var first = await service.DeliverAsync("tenant-A", registrationId, new DeliverRequest
        {
            EventType = "grade.posted",
            IdempotencyKey = "replay2",
            Payload = "{}",
        });

        await Assert.ThrowsAsync<UnauthorizedIntegrationOperationException>(
            () => service.ReplayDeadLetterAsync("tenant-B", "actor", first.DeliveryId!.Value));
    }
}
