using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Integrations.Models;

namespace OpenLearning.Integrations.Configuration;

public sealed class IntegrationRegistrationConfiguration : IEntityTypeConfiguration<IntegrationRegistration>
{
    public void Configure(EntityTypeBuilder<IntegrationRegistration> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Type).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CapabilitiesJson).HasMaxLength(4000).IsRequired();
        builder.Property(r => r.TenantId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.EndpointReference).HasMaxLength(2000);
        builder.Property(r => r.SecretReference).HasMaxLength(500);
        builder.HasIndex(r => new { r.TenantId, r.Type });
        builder.HasIndex(r => r.TenantId);
    }
}

public sealed class IntegrationDeliveryConfiguration : IEntityTypeConfiguration<IntegrationDelivery>
{
    public void Configure(EntityTypeBuilder<IntegrationDelivery> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(d => d.EventType).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PayloadReference).HasMaxLength(4000).IsRequired();
        builder.Property(d => d.TenantId).HasMaxLength(450).IsRequired();
        builder.HasIndex(d => new { d.TenantId, d.IdempotencyKey }).IsUnique();
        builder.HasIndex(d => d.RegistrationId);
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => d.Status);
    }
}

public sealed class IntegrationAuditRecordConfiguration : IEntityTypeConfiguration<IntegrationAuditRecord>
{
    public void Configure(EntityTypeBuilder<IntegrationAuditRecord> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Disposition).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Detail).HasMaxLength(2000);
        builder.Property(a => a.RedactedPayload).HasMaxLength(4000);
        builder.Property(a => a.ActorId).HasMaxLength(450);
        builder.Property(a => a.TenantId).HasMaxLength(450).IsRequired();
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.RegistrationId);
        builder.HasIndex(a => a.DeliveryId);
    }
}
