using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Payments.Models;

namespace OpenLearning.Payments.Configuration;

public sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Provider).HasMaxLength(40);
        builder.Property(x => x.ProviderIntentId).HasMaxLength(160);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => new { x.Provider, x.ProviderIntentId }).IsUnique();
    }
}

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{ public void Configure(EntityTypeBuilder<PaymentAttempt> builder) { builder.Property(x => x.ProviderReference).HasMaxLength(160); builder.HasIndex(x => x.PaymentIntentId); } }
public sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{ public void Configure(EntityTypeBuilder<PaymentRefund> builder) { builder.Property(x => x.Amount).HasPrecision(18, 2); builder.Property(x => x.ProviderRefundId).HasMaxLength(160); builder.Property(x => x.RequestedBy).HasMaxLength(450); builder.HasIndex(x => x.PaymentIntentId); } }
public sealed class ProviderEventConfiguration : IEntityTypeConfiguration<ProviderEvent>
{ public void Configure(EntityTypeBuilder<ProviderEvent> builder) { builder.Property(x => x.Provider).HasMaxLength(40); builder.Property(x => x.ProviderEventId).HasMaxLength(160); builder.Property(x => x.PayloadHash).HasMaxLength(64); builder.Property(x => x.EventType).HasMaxLength(80); builder.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique(); } }
public sealed class PaymentOutboxConfiguration : IEntityTypeConfiguration<PaymentOutbox>
{ public void Configure(EntityTypeBuilder<PaymentOutbox> builder) { builder.Property(x => x.Kind).HasMaxLength(80); builder.HasIndex(x => new { x.PaymentIntentId, x.Kind }).IsUnique(); } }
public sealed class PaymentReconciliationIssueConfiguration : IEntityTypeConfiguration<PaymentReconciliationIssue>
{ public void Configure(EntityTypeBuilder<PaymentReconciliationIssue> builder) { builder.Property(x => x.Reason).HasMaxLength(500); builder.HasIndex(x => x.PaymentIntentId); } }
