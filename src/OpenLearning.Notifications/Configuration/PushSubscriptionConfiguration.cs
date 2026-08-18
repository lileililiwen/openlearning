using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Configuration;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasIndex(s => new { s.UserId, s.Endpoint }).IsUnique();
        builder.Property(s => s.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(s => s.P256Dh).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Auth).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => s.UserId);
    }
}
