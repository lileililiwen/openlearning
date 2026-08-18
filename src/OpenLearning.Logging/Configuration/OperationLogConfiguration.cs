using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Logging.Models;

namespace OpenLearning.Logging.Configuration;

public class OperationLogConfiguration : IEntityTypeConfiguration<OperationLog>
{
    public void Configure(EntityTypeBuilder<OperationLog> builder)
    {
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.Action);
        builder.Property(l => l.ActorName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Action).HasMaxLength(100).IsRequired();
        builder.Property(l => l.TargetType).HasMaxLength(100);
        builder.Property(l => l.TargetId).HasMaxLength(100);
        builder.Property(l => l.Details).HasMaxLength(1000);
        builder.Property(l => l.IpAddress).HasMaxLength(64);
    }
}
