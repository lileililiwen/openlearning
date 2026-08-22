using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Lti.Models;

namespace OpenLearning.Lti.Configuration;

public sealed class LtiRegistrationConfiguration : IEntityTypeConfiguration<LtiRegistration>
{
    public void Configure(EntityTypeBuilder<LtiRegistration> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Issuer).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AuthorizationEndpoint).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.JwksUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TokenEndpoint).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Issuer, x.ClientId }).IsUnique();
    }
}
public sealed class LtiDeploymentConfiguration : IEntityTypeConfiguration<LtiDeployment>
{
    public void Configure(EntityTypeBuilder<LtiDeployment> builder) { builder.Property(x => x.DeploymentId).HasMaxLength(300).IsRequired(); builder.HasIndex(x => new { x.RegistrationId, x.DeploymentId }).IsUnique(); builder.HasOne(x => x.Registration).WithMany(x => x.Deployments).HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class LtiContextMappingConfiguration : IEntityTypeConfiguration<LtiContextMapping>
{
    public void Configure(EntityTypeBuilder<LtiContextMapping> builder) { builder.Property(x => x.ExternalContextId).HasMaxLength(300).IsRequired(); builder.HasIndex(x => new { x.DeploymentId, x.ExternalContextId }).IsUnique(); builder.HasOne(x => x.Deployment).WithMany(x => x.ContextMappings).HasForeignKey(x => x.DeploymentId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class LtiSubjectConfiguration : IEntityTypeConfiguration<LtiSubject>
{
    public void Configure(EntityTypeBuilder<LtiSubject> builder) { builder.Property(x => x.Subject).HasMaxLength(300).IsRequired(); builder.Property(x => x.UserId).HasMaxLength(450); builder.HasIndex(x => new { x.DeploymentId, x.Subject }).IsUnique(); }
}
public sealed class LtiResourceLinkConfiguration : IEntityTypeConfiguration<LtiResourceLink>
{
    public void Configure(EntityTypeBuilder<LtiResourceLink> builder) { builder.Property(x => x.ResourceLinkId).HasMaxLength(300).IsRequired(); builder.Property(x => x.Title).HasMaxLength(200); builder.Property(x => x.TargetUrl).HasMaxLength(2000).IsRequired(); builder.HasIndex(x => new { x.ContextMappingId, x.ResourceLinkId }).IsUnique(); }
}
public sealed class LtiLineItemConfiguration : IEntityTypeConfiguration<LtiLineItem>
{
    public void Configure(EntityTypeBuilder<LtiLineItem> builder) { builder.Property(x => x.ExternalLineItemId).HasMaxLength(500).IsRequired(); builder.Property(x => x.MaximumScore).HasPrecision(12, 4); builder.HasIndex(x => new { x.ContextMappingId, x.ExternalLineItemId }).IsUnique(); builder.HasOne(x => x.ContextMapping).WithMany().HasForeignKey(x => x.ContextMappingId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class LtiProtocolTokenConfiguration : IEntityTypeConfiguration<LtiProtocolToken>
{
    public void Configure(EntityTypeBuilder<LtiProtocolToken> builder) { builder.Property(x => x.Kind).HasMaxLength(20).IsRequired(); builder.Property(x => x.ValueHash).HasMaxLength(64).IsRequired(); builder.HasIndex(x => new { x.RegistrationId, x.Kind, x.ValueHash }).IsUnique(); }
}
public sealed class LtiSigningKeyConfiguration : IEntityTypeConfiguration<LtiSigningKey>
{
    public void Configure(EntityTypeBuilder<LtiSigningKey> builder) { builder.Property(x => x.KeyId).HasMaxLength(100).IsRequired(); builder.Property(x => x.PrivateKeyPem).IsRequired(); builder.Property(x => x.PublicKeyPem).IsRequired(); builder.HasIndex(x => x.KeyId).IsUnique(); }
}
public sealed class LtiAuditEventConfiguration : IEntityTypeConfiguration<LtiAuditEvent>
{
    public void Configure(EntityTypeBuilder<LtiAuditEvent> builder) { builder.Property(x => x.EventType).HasMaxLength(100).IsRequired(); builder.Property(x => x.Detail).HasMaxLength(2000).IsRequired(); builder.Property(x => x.CorrelationId).HasMaxLength(100); builder.HasIndex(x => x.CreatedAt); }
}
public sealed class LtiScoreOperationConfiguration : IEntityTypeConfiguration<LtiScoreOperation>
{
    public void Configure(EntityTypeBuilder<LtiScoreOperation> builder) { builder.Property(x => x.OperationId).HasMaxLength(200).IsRequired(); builder.Property(x => x.Subject).HasMaxLength(300).IsRequired(); builder.Property(x => x.Score).HasPrecision(12, 4); builder.HasIndex(x => new { x.LineItemId, x.OperationId }).IsUnique(); }
}
