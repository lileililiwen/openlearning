using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Logging.Models;

namespace OpenLearning.Logging.Configuration;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.HasIndex(l => l.CreatedAt);
        builder.Property(l => l.Message).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.Path).HasMaxLength(500);
        builder.Property(l => l.RequestMethod).HasMaxLength(20);
        builder.Property(l => l.UserId).HasMaxLength(450);
    }
}
